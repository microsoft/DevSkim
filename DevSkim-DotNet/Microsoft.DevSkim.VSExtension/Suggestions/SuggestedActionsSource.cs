// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DevSkim.VSExtension
{
    internal class SuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly SuggestedActionsSourceProvider _factory;
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;
        private SkimChecker _skimChecker;

        public SuggestedActionsSource(SuggestedActionsSourceProvider testSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            _factory = testSuggestedActionsSourceProvider;
            _textBuffer = textBuffer;
            _textView = textView;
        }

#pragma warning disable 0067

        public event EventHandler<EventArgs> SuggestedActionsChanged;

#pragma warning restore 0067

        public void Dispose()
        {
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            DevSkimError error = GetErrorUnderCaret(range);
            if (error != null && error.Actionable)
            {
                List<ISuggestedAction> fixActions = new List<ISuggestedAction>();
                var line = error.Span.Snapshot.GetLineFromPosition(error.Span.Start);

                ITrackingSpan errorSpan = error.ErrorTrackingSpan;

                // Create list of fixes if the rule has them..
                if (error.Rule.Fixes != null)
                {
                    fixActions.AddRange(error.Rule.Fixes.Select(fix => new FixSuggestedAction(errorSpan, error.Rule, fix)));
                }

                int suppressDays = Settings.GetSettings().SuppressDays;

                List<ISuggestedAction> suppActions = new List<ISuggestedAction>();
                var lineSpan = error.LineTrackingSpan;

                suppActions.Add(new SuppressSuggestedAction(error, suppressDays));
                suppActions.Add(new SuppressSuggestedAction(error));

                // If there is multiple issues on the line, offer "Suppress all"
                if (SkimShim.HasMultipleProblems(lineSpan.GetText(range.Snapshot),
                    lineSpan.TextBuffer.ContentType.TypeName))
                {
                    suppActions.Add(new SuppressSuggestedAction(error, suppressDays, true));
                    suppActions.Add(new SuppressSuggestedAction(error, suppressAll: true));
                }

                VSPackage.LogEvent(string.Format("Lightbulb invoked on {0} {1}", error.Rule.Id, error.Rule.Name));

                // We don't want empty group and spacer in the pop-up menu
                if (fixActions.Count > 0)
                    return new SuggestedActionSet[] { new SuggestedActionSet(fixActions), new SuggestedActionSet(suppActions) };
                else
                    return new SuggestedActionSet[] { new SuggestedActionSet(suppActions) };
            }

            return Enumerable.Empty<SuggestedActionSet>();
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                DevSkimError error = GetErrorUnderCaret(range);

                return (error != null && error.Actionable);
            });
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private bool TryGetSkimChecker()
        {
            if (_skimChecker != null)
                return true;

            return _textBuffer.Properties.TryGetProperty(typeof(SkimChecker), out _skimChecker);
        }

        private DevSkimError GetErrorUnderCaret(SnapshotSpan range)
        {
            if (TryGetSkimChecker())
            {
                DevSkimErrorsSnapshot securityErrors = _skimChecker.LastSecurityErrors;
                if (securityErrors != null)
                {
                    foreach (var error in securityErrors.Errors)
                    {
                        if (range.IntersectsWith(error.Span))
                        {
                            return error;
                        }
                    }
                }
            }

            return null;
        }
    }
}