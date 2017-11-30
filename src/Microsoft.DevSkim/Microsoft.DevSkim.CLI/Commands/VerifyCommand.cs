// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class VerifyCommand : ICommand
   {
        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Verify integrity and syntax of rules";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");

            command.OnExecute(() => {
                return (new VerifyCommand(locationArgument.Value)).Run();                
            });
        }

        public VerifyCommand(string path)
        {
            _path = path;
        }

        public int Run()
        {            
            Verifier verifier = new Verifier(_path);
            if (verifier.Verify())
            {
                Console.Error.WriteLine("No errors found.");
                return (int)ExitCode.NoIssues;
            }

            return (int)ExitCode.IssuesExists;
        }

        private string _path;
    }
}
