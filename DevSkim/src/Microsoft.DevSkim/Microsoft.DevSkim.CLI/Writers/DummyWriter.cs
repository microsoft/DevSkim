// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.DevSkim.CLI.Writers
{
    public class DummyWriter : Writer
    {
        public override void WriteIssue(IssueRecord issue)
        {
            // This is intentionaly empty
        }

        public override void FlushAndClose()
        {
            // This is intentionaly empty
        }

    }
}
