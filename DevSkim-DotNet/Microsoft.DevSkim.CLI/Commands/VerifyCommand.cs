// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class VerifyCommand
    {
        private readonly VerifyCommandOptions _opts;

        public VerifyCommand(VerifyCommandOptions options)
        {
            _opts = options;
        }
        
        public int Run()
        {
            DevSkimRuleSet devSkimRuleSet = new();

            foreach (var path in _opts.Rules)
            {
                devSkimRuleSet.AddPath(path);
            }

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