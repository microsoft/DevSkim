// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Linq;
using CommandLine;
using Microsoft.DevSkim.CLI.Commands;
using Microsoft.DevSkim.CLI.Options;

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
                    errs => errs.All(x => x.Tag == ErrorType.VersionRequestedError || x.Tag == ErrorType.HelpRequestedError) ? 0 : 1);
        }
    }
}