// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class VerifyCommand : ICommand
    {
        public VerifyCommand(string path, bool checkGuidanceOnline = false)
        {
            _path = path;
            _checkGuidance = checkGuidanceOnline;
        }

        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Verify integrity and syntax of rules";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");
            var checkGuidance = command.Option("-c", "Check online to see if guidance documents referenced exist.", CommandOptionType.NoValue);
            command.OnExecute(() =>
            {
                return (new VerifyCommand(locationArgument.Value, checkGuidance.HasValue())).Run();
            });
        }

        public int Run()
        {
            Verifier verifier = new Verifier(_path);
            if (verifier.Verify(_checkGuidance))
            {
                Console.WriteLine("No errors found.");
                return (int)ExitCode.NoIssues;
            }
            else
            {
                Console.Error.WriteLine("Errors found.");
                foreach(var message in verifier.Messages)
                {
                    Console.WriteLine(message.Message);
                }
            }
            return (int)ExitCode.IssuesExists;
        }

        private string _path;
        private bool _checkGuidance;
    }
}