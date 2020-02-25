// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.CLI
{
    public class IssueRecord
    {
        public string Filename { get; set; }
        public int Filesize { get; set; }
        public string TextSample { get; set; }
        public Issue Issue { get; set; }
    }
}
