// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using Microsoft.Extensions.CommandLineUtils;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Microsoft.CodeAnalysis.Sarif;
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
            var sarifLog = SarifLog.Load(_opts.SarifInput);
            if (sarifLog.Runs.Count > 0)
            {
                var run = sarifLog.Runs[0];
                var groupedResults = run.Results.GroupBy(x => x.Locations[0].PhysicalLocation.ArtifactLocation.Uri);
                if (!_opts.ApplyAllFixes && !_opts.FilesToApplyTo.Any() && !_opts.RulesToApplyFrom.Any())
                {
                    Console.WriteLine("Must specify either apply all fixes or a combination of file and rules to apply");
                    return (int)ExitCode.CriticalError;
                }

                if (_opts.FilesToApplyTo.Any())
                {
                    groupedResults =
                        groupedResults.Where(x => _opts.FilesToApplyTo.Any(y => x.Key.AbsolutePath.Contains(y)));
                }

                if (_opts.RulesToApplyFrom.Any())
                {
                    groupedResults = groupedResults.Where(x => 
                        _opts.RulesToApplyFrom.Any(y => 
                            x.Any(z => 
                                z.RuleId == y)));
                }

                groupedResults = groupedResults.ToList();
                foreach (var resultGroup in groupedResults)
                {
                    var fileName = resultGroup.Key;

                    // Flatten all the replacements into a single list
                    var listOfReplacements = resultGroup.SelectMany(x =>
                        x.Fixes.SelectMany(y => y.ArtifactChanges)
                            .SelectMany(z => z.Replacements)).ToList();
                    // Order the results by the character offset
                    listOfReplacements.Sort((a, b) => a.DeletedRegion.CharOffset - b.DeletedRegion.CharOffset);
                    
                    // TODO: Support path rooting
                    if (File.Exists(fileName.AbsolutePath))
                    {
                        var theContent = File.ReadAllText(fileName.AbsolutePath);
                        // CurPos tracks the current position in the original string
                        int curPos = 0;
                        var sb = new StringBuilder();
                        foreach (var replacement in listOfReplacements)
                        {
                            // The replacements were sorted, so this indicates a second replacement option for the same region
                            // TODO: Improve a way to not always take the first replacement, perhaps using tags for ranking
                            if (replacement.DeletedRegion.CharOffset < curPos)
                            {
                                continue;
                            }
                            sb.Append(theContent[curPos..replacement.DeletedRegion.CharOffset]);
                            sb.Append(replacement.InsertedContent.Text);
                            curPos = replacement.DeletedRegion.CharOffset + replacement.DeletedRegion.CharLength;
                        }

                        sb.Append(theContent[curPos..]);
                        File.WriteAllText(fileName.AbsolutePath, sb.ToString());
                    }
                }
            }
            
            return (int)ExitCode.NoIssues;
        }
    }
}