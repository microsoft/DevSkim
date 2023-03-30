// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class DevSkimSuggestedAction: ISuggestedAction
    {
        public DevSkimSuggestedAction(SnapshotSpan span, CodeFixMapping mapping)
        {
            _span = span;
            _snapshot = span.Snapshot;
            _mapping = mapping;
            _display = mapping.friendlyString;
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

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = _snapshot.GetLineFromPosition(_span.Start.Position);

            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = _mapping.replacement, Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xAF, 0x00)) });
            return Task.FromResult<object>(textBlock);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _span.Snapshot.TextBuffer.Replace(_span, _mapping.replacement);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private readonly CodeFixMapping _mapping;
        private readonly string _display;
        private readonly ITextSnapshot _snapshot;
        private readonly SnapshotSpan _span;
    }
}