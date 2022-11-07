// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using CommandLine;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim.CLI.Commands;
using Microsoft.DevSkim.CLI.Options;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.DevSkim.CLI
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<AnalyzeCommandOptions, FixCommandOptions, VerifyCommandOptions>(args)
                .MapResult(
                    (AnalyzeCommandOptions opts) => new AnalyzeCommand(opts).Run(),
                    (FixCommandOptions opts) => new FixCommand(opts).Run(),
                    (VerifyCommandOptions opts) => new VerifyCommand(opts).Run(),
                    errs => 1);
        }
    }
}