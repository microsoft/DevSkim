// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim.VSExtension
{
    public class DevSkimError
    {
        public readonly SnapshotSpan Span;
        public readonly Rule Rule;
        public readonly bool Actionable;

        // This is used by SecurityErrorsSnapshot.TranslateTo() to map this error to the corresponding error in the next snapshot.
        public int NextIndex = -1;

        public DevSkimError(SnapshotSpan span, Rule rule, bool actionable)
        {
            this.Span = span;
            this.Rule = rule;
            this.Actionable = actionable;
        }

        public ITrackingSpan ErrorTrackingSpan
        {
            get
            {
                return Span.Snapshot.CreateTrackingSpan(Span, SpanTrackingMode.EdgeInclusive);
            }
        }

        public ITrackingSpan LineTrackingSpan
        {
            get
            {
                return Span.Snapshot.CreateTrackingSpan(Span.Snapshot.GetLineFromPosition(Span.Start).Extent, SpanTrackingMode.EdgeInclusive);
            }
        }

        public ITextSnapshot Snapshot
        {
            get
            {
                return this.Span.Snapshot;   
            }
        }

        public string LineText 
        { 
            get
            {
                return LineTrackingSpan.GetText(Snapshot);
            }
        }

        public string ErrorText
        {
            get
            {
                return ErrorTrackingSpan.GetText(Snapshot);
            }
        }

        public int LineNumber
        {
            get
            {
                return Snapshot.GetLineNumberFromPosition(Span.Start);
            }
        }

        public ITrackingSpan LineAndSuppressionCommentTrackingSpan { 
            get
            {
                var reg = new Regex(Suppression.pattern);
                if (reg.IsMatch(LineText))
                {
                    return LineTrackingSpan;
                }

                var end = LineTrackingSpan.GetEndPoint(Snapshot).Position;
                if (LineNumber > 1)
                {
                    var content = Snapshot.GetLineFromLineNumber(LineNumber - 1);
                    if (content.GetText().Contains(Language.GetCommentInline(Span.Snapshot.ContentType.TypeName)))
                    {
                        if (reg.IsMatch(content.GetText()))
                        {
                            return Snapshot.CreateTrackingSpan(content.Start.Position, end - content.Start.Position, SpanTrackingMode.EdgeInclusive);
                        }
                    }
                    if (content.GetText().Contains(Language.GetCommentSuffix(Snapshot.ContentType.TypeName)))
                    {
                        bool foundSuppression = false;

                        for (var i = LineNumber - 1; i > 0; i--)
                        {
                            content = Snapshot.GetLineFromLineNumber(i);

                            if (reg.IsMatch(content.GetText()))
                            {
                                foundSuppression = true;
                            }
                            if (content.GetText().Contains(Language.GetCommentPrefix(Span.Snapshot.ContentType.TypeName)))
                            {
                                if (foundSuppression)
                                {
                                    return Snapshot.CreateTrackingSpan(content.Start.Position, end - content.Start.Position, SpanTrackingMode.EdgeInclusive);
                                }
                                else
                                {
                                    return LineTrackingSpan;
                                }
                            }
                        }
                    }
                }

                return LineTrackingSpan;
            }
        }

        public static DevSkimError Clone(DevSkimError error)
        {
            return new DevSkimError(error.Span, error.Rule, error.Actionable);
        }

        public static DevSkimError CloneAndTranslateTo(DevSkimError error, ITextSnapshot newSnapshot)
        {
            var newSpan = error.Span.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive);

            // We want to only translate the error if the length of the error span did not change (if it did change, it would imply that
            // there was some text edit inside the error and, therefore, that the error is no longer valid).
            return (newSpan.Length == error.Span.Length)
                   ? new DevSkimError(newSpan, error.Rule, error.Actionable)
                   : null;
        }
    }
}
