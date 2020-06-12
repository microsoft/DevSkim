// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class TestCommand : ICommand
    {
        public TestCommand(string path, bool coverage)
        {
            _path = path;
            _coverage = coverage;
        }

        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Run tests for rules";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");

            var coverageOption = command.Option("-c|--coverage",
                                              "Test coverage information",
                                              CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                return (new TestCommand(locationArgument.Value,
                                        coverageOption.HasValue())).Run();
            });
        }

        public int Run()
        {
            if (!Directory.Exists(_path))
            {
                Console.Error.WriteLine("Error: Not a valid file or directory {0}", _path);
                return (int)ExitCode.CriticalError;
            }

            Verifier verifier = new Verifier(_path);
            if (!verifier.Verify())
                return (int)ExitCode.IssuesExists;

            Tester tester = new Tester(verifier.CompiledRuleset);
            tester.DoCoverage = _coverage;
            return tester.Run(_path);
        }

        private bool _coverage;
        private string _path;
    }
}