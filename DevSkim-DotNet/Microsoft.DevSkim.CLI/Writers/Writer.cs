// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.DevSkim.CLI.Writers
{
    public abstract class Writer : IDisposable
    {
#nullable disable

        public StreamWriter TextWriter { get; set; }
#nullable restore

        public abstract void FlushAndClose();

        public abstract void WriteIssue(IssueRecord issue);

        public void Dispose()
        {
            TextWriter.Dispose();
        }
    }
}