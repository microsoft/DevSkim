// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.DevSkim.LanguageProtoInterop;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Experimentation;
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
                if (StaticData.FileToCodeFixMap.TryGetValue(new Uri(_fileName), out System.Collections.Concurrent.ConcurrentDictionary<int, HashSet<CodeFixMapping>> dictForFile))
                {
                    if (dictForFile.TryGetValue(wordExtent.Span.Snapshot.Version.VersionNumber, out HashSet<CodeFixMapping> fixes))
                    {
                        suggestedActions.AddRange(fixes.Where(codeFixMapping => Intersects(codeFixMapping, wordExtent)).Select(intersectedMapping => new DevSkimSuggestedAction(wordExtent.Span, intersectedMapping)));
                    }
                }
                yield return new SuggestedActionSet("DevSkim Suggestions", suggestedActions, applicableToSpan: wordExtent.Span);
            }
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

        // Map FileName to Mapping of FileVersion to Potential fixes
        // This should come from DevSkimFixMessageTarget or StaticScannerSettings
        private Dictionary<string, Dictionary<int, HashSet<CodeFixMapping>>> _bucket_o_suggestions = new Dictionary<string, Dictionary<int, HashSet<CodeFixMapping>>>();

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                bool res = TryGetWordUnderCaret(out TextExtent wordExtent);
                if (res && wordExtent.IsSignificant)
                {
                    if (StaticData.FileToCodeFixMap.TryGetValue(new Uri(_fileName), out System.Collections.Concurrent.ConcurrentDictionary<int, HashSet<CodeFixMapping>> dictForFile))
                    {
                        if (dictForFile.TryGetValue(wordExtent.Span.Snapshot.Version.VersionNumber, out HashSet<CodeFixMapping> fixes))
                        {
                            return fixes.Any(codeFixMapping => Intersects(codeFixMapping, wordExtent));
                        }
                    }
                }
                return false;
            }, new CancellationTokenSource().Token, TaskCreationOptions.None, TaskScheduler.Default);
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