// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.DevSkim
{
    public class IssueRecord
    {
        public IssueRecord(string Filename, int Filesize, string TextSample, Issue Issue, string Language)
        {
            this.Filename = Filename;
            this.Filesize = Filesize;
            this.TextSample = TextSample;
            this.Issue = Issue;
            this.Language = Language;
        }
        public string Filename { get; }
        public int Filesize { get; }
        public Issue Issue { get; }
        public string Language { get; }
        public string TextSample { get; }
        
        /// <summary>
        /// Set by the processor if a Fix was applied for this issue
        /// </summary>
        public bool Fixed { get; set; }
        
        /// <summary>
        /// The fix which was applied
        /// </summary>
        public CodeFix FixApplied { get; set; }
    }
}