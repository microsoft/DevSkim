// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.DevSkim.VSExtension
{
    internal class FixSuggestedAction : ISuggestedAction
    {
        public FixSuggestedAction(ITrackingSpan span, Rule rule, CodeFix fix)
        {
            _rule = rule;
            _fix = fix;
            _span = span;
            _snapshot = span.TextBuffer.CurrentSnapshot;
            string code = span.GetText(_snapshot);
            _fixedCode = RuleProcessor.Fix(code, _fix);
            _display = (string.IsNullOrEmpty(_fix.Name)) ? _fixedCode : _fix.Name;
        }

        public string DisplayText
        {
            get
            {
                return _display;
            }
        }

        public bool HasActionSets
        {
            get
            {
                return false;
            }
        }

        public bool HasPreview
        {
            get
            {
                return false;
            }
        }

        public string IconAutomationText
        {
            get
            {
                return null;
            }
        }

        ImageMoniker ISuggestedAction.IconMoniker
        {
            get
            {
                return default(ImageMoniker);
            }
        }

        public string InputGestureText
        {
            get
            {
                return null;
            }
        }

        public void Dispose()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD114:Avoid returning a null Task", Justification = "Documentation specifies this should return null. https://github.com/microsoft/vs-editor-api/blob/61ca43c05f2254b80e87dccaf029acec3f25feb3/src/Editor/Language/Def/Intellisense/Suggestions/ISuggestedAction.cs#L39 ")]
        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var snapShot = _span.TextBuffer.CurrentSnapshot;
            var theSpan = _span.GetSpan(snapShot);
            var line = snapShot.GetLineFromPosition(_span.GetStartPoint(snapShot).Position);

            SnapshotSpan preSpan = new SnapshotSpan(line.Start, theSpan.Start);
            SnapshotSpan postSpan = new SnapshotSpan(theSpan.End, line.End);

            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = preSpan.GetText() });
            textBlock.Inlines.Add(new Run() { Text = _fixedCode, Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xAF, 0x00)) });
            textBlock.Inlines.Add(new Run() { Text = postSpan.GetText() });
            return Task.FromResult<object>(textBlock);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _span.TextBuffer.Replace(_span.GetSpan(_snapshot), _fixedCode);

            VSPackage.LogEvent(string.Format("Fix invoked on {0} {1} fix {2}", _rule.Id, _rule.Name, _fix.Name));
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private readonly string _display;
        private readonly CodeFix _fix;
        private readonly string _fixedCode;
        private readonly Rule _rule;
        private readonly ITextSnapshot _snapshot;
        private readonly ITrackingSpan _span;
    }
}