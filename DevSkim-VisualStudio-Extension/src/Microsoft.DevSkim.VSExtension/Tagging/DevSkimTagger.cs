// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace Microsoft.DevSkim.VSExtension
{
    class DevSkimTagger : ITagger<DevSkimTag>, IDisposable
    {
        private readonly SkimChecker _skimChecker;
        private DevSkimErrorsSnapshot _securityErrors;

        internal DevSkimTagger(SkimChecker skimhecker)
        {
            _skimChecker = skimhecker;
            _securityErrors = skimhecker.LastSecurityErrors;
            skimhecker.AddTagger(this);
        }

        internal void UpdateErrors(ITextSnapshot currentSnapshot, DevSkimErrorsSnapshot securityErrors)
        {
            var oldSecurityErrors = _securityErrors;
            _securityErrors = securityErrors;

            var h = this.TagsChanged;
            if (h != null)
            {
                // Raise a single tags changed event over the span that could have been affected by the change in the errors.
                int start = int.MaxValue;
                int end = int.MinValue;

                if ((oldSecurityErrors != null) && (oldSecurityErrors.Errors.Count > 0))
                {
                    start = oldSecurityErrors.Errors[0].Span.Start.TranslateTo(currentSnapshot, PointTrackingMode.Negative);
                    end = oldSecurityErrors.Errors[oldSecurityErrors.Errors.Count - 1].Span.End.TranslateTo(currentSnapshot, PointTrackingMode.Positive);
                }

                if (securityErrors.Count > 0)
                {
                    start = Math.Min(start, securityErrors.Errors[0].Span.Start.Position);
                    end = Math.Max(end, securityErrors.Errors[securityErrors.Errors.Count - 1].Span.End.Position);
                }

                if (start < end)
                {
                    h(this, new SnapshotSpanEventArgs(new SnapshotSpan(currentSnapshot, Span.FromBounds(start, end))));
                }
            }
        }

        public void Dispose()
        {
            // Called when the tagger is no longer needed (generally when the ITextView is closed).
            _skimChecker.RemoveTagger(this);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<DevSkimTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (_securityErrors != null)
            {
                foreach (var error in _securityErrors.Errors)
                {
                    if (spans.IntersectsWith(error.Span))
                    {
                        yield return new TagSpan<DevSkimTag>(error.Span, new DevSkimTag(error.Rule, error.Actionable));
                    }
                }
            }
        }
    }
}
