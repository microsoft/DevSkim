// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
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
            if (!string.IsNullOrEmpty(_opts.CommentsPath) ^ !string.IsNullOrEmpty(_opts.LanguagesPath))
            {
                Console.WriteLine("If languages or comments are specified both must be specified.");
                return (int)ExitCode.ArgumentParsingError;
            }
            
            DevSkimRuleSet devSkimRuleSet = new();

            foreach (string path in _opts.Rules)
            {
                devSkimRuleSet.AddPath(path);
            }

            DevSkimRuleVerifier devSkimVerifier = new DevSkimRuleVerifier(new DevSkimRuleVerifierOptions()
            {
                LanguageSpecs = !string.IsNullOrEmpty(_opts.CommentsPath) && !string.IsNullOrEmpty(_opts.LanguagesPath) ? DevSkimLanguages.FromFiles(_opts.CommentsPath, _opts.LanguagesPath) : new Languages()
                //TODO: Add logging factory to get validation errors.
            });

            DevSkimRulesVerificationResult result = devSkimVerifier.Verify(devSkimRuleSet);

            if (!result.Verified)
            {
                Console.WriteLine("Error: Rules failed validation. ");
                foreach (RuleStatus status in result.DevSkimRuleStatuses)
                {
                    if (!status.Verified)
                    {
                        foreach (string error in status.Errors)
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