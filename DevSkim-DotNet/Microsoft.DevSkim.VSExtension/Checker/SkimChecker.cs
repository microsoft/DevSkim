// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace Microsoft.DevSkim.VSExtension
{
    ///<summary>
    /// Finds the security errors in comments for a specific buffer.
    ///</summary>
    /// <remarks><para>The lifespan of this object is tied to the lifespan of the taggers on the view. On creation of the first tagger, the SkimChecker starts doing
    /// work to find errors in the file. On the disposal of the last tagger, it shuts down.</para>
    /// </remarks>
    public class SkimChecker
    {
        public DevSkimErrorsSnapshot LastSecurityErrors;

        internal readonly DevSkimErrorsFactory Factory;
        internal string FilePath;

        internal SkimChecker(DevSkimProvider provider, ITextView textView, ITextBuffer buffer)
        {
            _provider = provider;
            _buffer = buffer;
            _textView = textView;
            _currentSnapshot = buffer.CurrentSnapshot;

            // Get the name of the underlying document buffer
            ITextDocument document;
            if (provider.TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                this.FilePath = document.FilePath;
                // Watch out for file name being changed
                document.FileActionOccurred += Document_FileActionOccurred;
            }

            // We're assuming we're created on the UI thread so capture the dispatcher so we can do all of our
            // updates on the UI thread.
            _uiThreadDispatcher = Dispatcher.CurrentDispatcher;

            this.Factory = new DevSkimErrorsFactory(this, new DevSkimErrorsSnapshot(this.FilePath, 0));
        }

        internal void AddTagger(DevSkimTagger tagger)
        {
            _activeTaggers.Add(tagger);

            if (_activeTaggers.Count == 1)
            {
                // First tagger created ... start doing stuff.
                _classifier = _provider.ClassifierAggregatorService.GetClassifier(_buffer);

                _buffer.ChangedLowPriority += this.OnBufferChange;

                _dirtySpans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(_currentSnapshot, 0, _currentSnapshot.Length));

                _provider.AddSkimChecker(this);

                this.KickUpdate();
            }
        }

        /// <summary>
        ///     Watch out for file name changes
        /// </summary>
        internal void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (this.FilePath != e.FilePath)
            {
                this.FilePath = e.FilePath;
            }
        }

        internal void RemoveTagger(DevSkimTagger tagger)
        {
            _activeTaggers.Remove(tagger);

            if (_activeTaggers.Count == 0)
            {
                // Last tagger was disposed of. This is means there are no longer any open views on the buffer
                // so we can safely shut down security checking for that buffer.
                _buffer.ChangedLowPriority -= this.OnBufferChange;

                _provider.RemoveSkimChecker(this);

                _isDisposed = true;

                _buffer.Properties.RemoveProperty(typeof(SkimChecker));

                IDisposable classifierDispose = _classifier as IDisposable;
                if (classifierDispose != null)
                    classifierDispose.Dispose();

                _classifier = null;
            }
        }

        private readonly List<DevSkimTagger> _activeTaggers = new List<DevSkimTagger>();
        private readonly ITextBuffer _buffer;
        private readonly DevSkimProvider _provider;
        private readonly ITextView _textView;
        private readonly Dispatcher _uiThreadDispatcher;

        private IClassifier _classifier;

        private ITextSnapshot _currentSnapshot;
        private NormalizedSnapshotSpanCollection _dirtySpans;

        private bool _isDisposed = false;
        private bool _isUpdating = false;

        /// <summary>
        ///     This is where we get the lines for parsing
        /// </summary>
        private void DoUpdate()
        {
            // It would be good to do all of this work on a background thread but we can't:
            // _classifier.GetClassificationSpans() should only be called on the UI thread because some
            // classifiers assume they are called from the UI thread. Raising the TagsChanged event from the
            // taggers needs to happen on the UI thread (because some consumers might assume it is being
            // raised on the UI thread).
            //
            // Updating the snapshot for the factory and calling the sink can happen on any thread but those
            // operations are so fast that there is no point.
            if ((!_isDisposed) && (_dirtySpans.Count > 0))
            {
                var line = _dirtySpans[0].Start.GetContainingLine();

                if (line.Length > 0)
                {
                    var oldSecurityErrors = Factory.CurrentSnapshot;
                    var newSecurityErrors = new DevSkimErrorsSnapshot(this.FilePath, oldSecurityErrors.VersionNumber + 1);

                    string text = _textView.TextSnapshot.GetText();

                    Issue[] issues = SkimShim.Analyze(text, line.Snapshot.ContentType.TypeName, FilePath);
                    // Don't populate suppressed issues
                    foreach (Issue issue in issues.Where(x => !x.IsSuppressionInfo))
                    {
                        int errorStart = issue.StartLocation.Column;
                        int errorLength = issue.Boundary.Length;
                        if (errorLength > 0)    // Ignore any single character error.
                        {
                            line = _textView.TextSnapshot.GetLineFromLineNumber(issue.StartLocation.Line - 1);
                            var newSpan = new SnapshotSpan(line.Start + errorStart - 1, errorLength);
                            var oldError = oldSecurityErrors.Errors.Find((e) => e.Span == newSpan);

                            if (oldError != null)
                            {
                                // There was a security error at the same span as the old one so we should be
                                // able to just reuse it.
                                oldError.NextIndex = newSecurityErrors.Errors.Count;
                                newSecurityErrors.Errors.Add(DevSkimError.Clone(oldError));    // Don't clone the old error yet
                            }
                            else
                            {
                                newSecurityErrors.Errors.Add(new DevSkimError(newSpan, issue.Rule, !issue.IsSuppressionInfo));
                            }
                        }
                    }

                    UpdateSecurityErrors(newSecurityErrors);
                }

                _dirtySpans = new NormalizedSnapshotSpanCollection(line.Snapshot, new NormalizedSpanCollection());
                _isUpdating = false;
            }
        }

        private void KickUpdate()
        {
            // TODO: Now called async - We're assuming we will only be called from the UI thread so there
            // should be no issues with race conditions.
            if (!_isUpdating)
            {
                _isUpdating = true;
                // TODO: Improve this
                _uiThreadDispatcher.BeginInvoke(new Action(() => this.DoUpdate()), DispatcherPriority.Background);
            }
        }

        private void OnBufferChange(object sender, TextContentChangedEventArgs e)
        {
            _currentSnapshot = e.After;

            // Translate all of the old dirty spans to the new snapshot.
            NormalizedSnapshotSpanCollection newDirtySpans = _dirtySpans.CloneAndTrackTo(e.After, SpanTrackingMode.EdgeInclusive);

            // Dirty all the spans that changed.
            foreach (var change in e.Changes)
            {
                newDirtySpans = NormalizedSnapshotSpanCollection.Union(newDirtySpans, new NormalizedSnapshotSpanCollection(e.After, change.NewSpan));
            }

            // Translate all the security errors to the new snapshot (and remove anything that is a dirty
            // region since we will need to check that again).
            var oldSecurityErrors = this.Factory.CurrentSnapshot;
            var newSecurityErrors = new DevSkimErrorsSnapshot(this.FilePath, oldSecurityErrors.VersionNumber + 1);

            // Copy all of the old errors to the new errors unless the error was affected by the text change
            foreach (var error in oldSecurityErrors.Errors)
            {
                Debug.Assert(error.NextIndex == -1);

                var newError = DevSkimError.CloneAndTranslateTo(error, e.After);

                if (newError != null)
                {
                    Debug.Assert(newError.Span.Length == error.Span.Length);

                    error.NextIndex = newSecurityErrors.Errors.Count;
                    newSecurityErrors.Errors.Add(newError);
                }
            }

            this.UpdateSecurityErrors(newSecurityErrors);

            _dirtySpans = newDirtySpans;

            // Start processing the dirty spans (which no-ops if we're already doing it).
            if (_dirtySpans.Count != 0)
            {
                this.KickUpdate();
            }
        }

        private void UpdateSecurityErrors(DevSkimErrorsSnapshot securityErrors)
        {
            // Tell our factory to snap to a new snapshot.
            this.Factory.UpdateErrors(securityErrors);

            LastSecurityErrors = securityErrors;

            // Tell the provider to mark all the sinks dirty (so, as a side-effect, they will start an update
            // pass that will get the new snapshot from the factory).
            SkimChecker skimChecker = new SkimChecker(this._provider, this._textView, this._buffer);
            skimChecker._isDisposed = true;
            _provider.UpdateAllSinks(skimChecker);

            foreach (var tagger in _activeTaggers)
            {
                tagger.UpdateErrors(_currentSnapshot, securityErrors);
            }
        }
    }
}