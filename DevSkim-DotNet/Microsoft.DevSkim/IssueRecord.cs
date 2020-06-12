// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.DevSkim
{
    public class IssueRecord
    {
        public IssueRecord(string Filename, int Filesize, string TextSample, Issue Issue)
        {
            this.Filename = Filename;
            this.Filesize = Filesize;
            this.TextSample = TextSample;
            this.Issue = Issue;
        }

        public string Filename { get; }
        public int Filesize { get; }
        public Issue Issue { get; }
        public string TextSample { get; }
    }
}