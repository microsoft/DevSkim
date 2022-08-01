// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.DevSkim.AI;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class DummyWriter : Writer
    {
        public override void FlushAndClose()
        {
            // This is intentionaly empty
        }

        public override void WriteIssue(IssueRecord issue)
        {
            // This is intentionaly empty
        }
    }
}