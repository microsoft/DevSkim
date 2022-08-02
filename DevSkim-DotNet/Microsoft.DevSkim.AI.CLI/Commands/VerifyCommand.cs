﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using Microsoft.DevSkim.AI;
using Microsoft.DevSkim.AI.CLI;
using Microsoft.DevSkim.AI.CLI.Commands;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class VerifyCommand : ICommand
    {
        private string _path;
        public VerifyCommand(string path)
        {
            _path = path;
        }

        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Verify integrity and syntax of rules";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");

            command.OnExecute(() =>
            {
                return (new VerifyCommand(locationArgument.Value)).Run();
            });
        }

        public int Run()
        {
            DevSkimRuleSet devSkimRuleSet = new();
            
            devSkimRuleSet.AddPath(_path);

            var devSkimVerifier = new DevSkimRuleVerifier(new DevSkimRuleVerifierOptions()
            {
                LanguageSpecs = DevSkimLanguages.LoadEmbedded()
                //TODO: Add logging factory to get validation errors.
            });

            var result = devSkimVerifier.Verify(devSkimRuleSet);

            if (!result.Verified)
            {
                Console.WriteLine("Error: Rules failed validation. ");
                foreach (var status in result.DevSkimRuleStatuses)
                {
                    if (!status.Verified)
                    {
                        foreach (var error in status.Errors)
                        {
                            Console.WriteLine(status.Errors);
                        }
                    }

                    return (int)ExitCode.IssuesExists;
                }
            }

            if (!devSkimRuleSet.Any())
            {
                Debug.WriteLine("Error: No rules were loaded. ");
                return (int)ExitCode.CriticalError;
            }
            
            return (int)ExitCode.NoIssues;
        }
    }
}