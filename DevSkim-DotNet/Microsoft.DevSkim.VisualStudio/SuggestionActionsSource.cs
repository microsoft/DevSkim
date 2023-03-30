// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.DevSkim.LanguageProtoInterop;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Microsoft.DevSkim.VisualStudio
{
    internal class SuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly SuggestedActionsSourceProvider _factory;
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;
        private readonly string _fileName;

        public SuggestedActionsSource(SuggestedActionsSourceProvider testSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            _factory = testSuggestedActionsSourceProvider;
            _textBuffer = textBuffer;
            _textView = textView;
            _fileName = _textBuffer.Properties.GetProperty<ITextDocument>(typeof(ITextDocument)).FilePath;
        }

#pragma warning disable 0067

        public event EventHandler<EventArgs> SuggestedActionsChanged;

#pragma warning restore 0067

        public void Dispose()
        {
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            List<ISuggestedAction> suggestedActions = new List<ISuggestedAction>();
            if (TryGetWordUnderCaret(out TextExtent wordExtent) && wordExtent.IsSignificant)
            {
                if (StaticData.FileToCodeFixMap.TryGetValue(new Uri(_fileName), out var dictForFile))
                {
                    var potentialFixesForFile = dictForFile[wordExtent.Span.Snapshot.Version.VersionNumber];
                    var filteredFixes = potentialFixesForFile.Where(codeFixMapping => Intersects(codeFixMapping, wordExtent));
                    foreach (var filtered in filteredFixes)
                    {
                        suggestedActions.Add(new DevSkimSuggestedAction(wordExtent.Span, filtered));
                    }
                }
            }
            // Code from DevSkim 0.7
            //if (error != null && error.Actionable)
            //{
            //    List<ISuggestedAction> fixActions = new List<ISuggestedAction>();
            //    var line = error.Span.Snapshot.GetLineFromPosition(error.Span.Start);

            //    ITrackingSpan errorSpan = error.ErrorTrackingSpan;

            //    // Create list of fixes if the rule has them..
            //    if (error.Rule.Fixes != null)
            //    {
            //        fixActions.AddRange(error.Rule.Fixes.Select(fix => new FixSuggestedAction(errorSpan, error.Rule, fix)));
            //    }

            //    int suppressDays = Settings.GetSettings().SuppressDays;

            //    List<ISuggestedAction> suppActions = new List<ISuggestedAction>();
            //    var lineSpan = error.LineTrackingSpan;

            //    suppActions.Add(new SuppressSuggestedAction(error, suppressDays));
            //    suppActions.Add(new SuppressSuggestedAction(error));

            //    // If there is multiple issues on the line, offer "Suppress all"
            //    if (SkimShim.HasMultipleProblems(lineSpan.GetText(range.Snapshot),
            //        lineSpan.TextBuffer.ContentType.TypeName))
            //    {
            //        suppActions.Add(new SuppressSuggestedAction(error, suppressDays, true));
            //        suppActions.Add(new SuppressSuggestedAction(error, suppressAll: true));
            //    }

            //    VSPackage.LogEvent(string.Format("Lightbulb invoked on {0} {1}", error.Rule.Id, error.Rule.Name));

            //    // We don't want empty group and spacer in the pop-up menu
            //    if (fixActions.Count > 0)
            //        return new SuggestedActionSet[] { new SuggestedActionSet(fixActions), new SuggestedActionSet(suppActions) };
            //    else
            //        return new SuggestedActionSet[] { new SuggestedActionSet(suppActions) };
            //}
            yield return new SuggestedActionSet(suggestedActions);
        }

        private bool Intersects(CodeFixMapping codeFixMapping, TextExtent wordExtent)
        {
            // Extent start is inside mapping
            if (wordExtent.Span.Start >= codeFixMapping.matchStart && wordExtent.Span.Start <= codeFixMapping.matchEnd)
            {
                return true;
            }
            // Extend end is inside mapping
            if (wordExtent.Span.End >= codeFixMapping.matchStart && wordExtent.Span.End <= codeFixMapping.matchEnd)
            {
                return true;
            }
            return false;
        }

        private bool Intersects(int start, int end, SnapshotSpan span)
        {
            return (span.Start.Position == start && span.End.Position == end);
        }

        // Map FileName to Mapping of FileVersion to Potential fixes
        // This should come from DevSkimFixMessageTarget or StaticScannerSettings
        private Dictionary<string, Dictionary<int, HashSet<CodeFixMapping>>> _bucket_o_suggestions = new Dictionary<string, Dictionary<int, HashSet<CodeFixMapping>>>();

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {  
            return Task.Factory.StartNew(() =>
            {
                var res = TryGetWordUnderCaret(out TextExtent wordExtent);
                return true;
                // Check that the extent isn't whitespace
                // And that there are any code fix mappings filename given for the version of that document given
                //return (wordExtent.IsSignificant &&
                //    _bucket_o_suggestions[fileName]
                //        .Any(x => x[wordExtent.Span.Snapshot.Version.VersionNumber]
                //            .Any(y => wordExtent.Span.IntersectsWith(y.diagnostic.Range))));
            });
        }

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            ITextCaret caret = _textView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default(TextExtent);
                return false;
            }

            ITextStructureNavigator navigator = _factory.NavigatorService.GetTextStructureNavigator(_textBuffer);

            wordExtent = navigator.GetExtentOfWord(point);
            return true;
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}