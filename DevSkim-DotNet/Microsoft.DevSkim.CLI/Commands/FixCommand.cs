// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using Microsoft.Extensions.CommandLineUtils;
using System.Diagnostics;
using System.Linq;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class FixCommand
    {
        private readonly FixCommandOptions _opts;

        public FixCommand(FixCommandOptions options)
        {
            _opts = options;
        }

        public int Run()
        {
            
            return (int)ExitCode.NoIssues;
        }
    }
}