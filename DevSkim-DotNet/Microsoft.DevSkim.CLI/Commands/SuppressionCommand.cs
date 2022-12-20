// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class SuppressionCommand
    {
        private readonly SuppressionCommandOptions _opts;

        public SuppressionCommand(SuppressionCommandOptions options)
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
                if (!_opts.ApplyAllSuppression && !_opts.FilesToApplyTo.Any() && !_opts.RulesToApplyFrom.Any())
                {
                    Console.WriteLine("Must specify either apply all suppression comments or a combination of file and rules to apply");
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
                    var potentialPath = Path.Combine(_opts.Path, fileName.OriginalString);
                    // Flatten all the replacements into a single list
                    var listOfReplacements = resultGroup
                      .Where(x => x.Locations is { })
                      .SelectMany(x => x.Locations).Select(x => x.PhysicalLocation).ToList();

                    listOfReplacements.Sort((a, b) => a.Region.StartLine - b.Region.StartLine);

                    var ruleIds = string.Join(",", resultGroup.Select(x => x.RuleId).Distinct());

                    if (File.Exists(potentialPath))
                    {
                        var theContent = File.ReadAllLines(potentialPath);
                        // CurPos tracks the current position in the original string
                        int currLine = 0;
                        var sb = new StringBuilder();

                        foreach (var replacement in listOfReplacements)
                        {
                            var zbStartLine = replacement.Region.StartLine - 1;
                            var zbEndLine = replacement.Region.EndLine - 1;
                            var endLine = Math.Min(zbEndLine + 1, theContent.Length);
                            
                            var isFirstLine = true;
                            foreach (var line in theContent[currLine..endLine])
                                if(isFirstLine){
                                    sb.Append(line);
                                    isFirstLine = false;
                                } else{
                                    sb.Append($"{Environment.NewLine}{line}");
                                }
                                    
                            var ignoreComment = $"{getInlineComment(replacement.Region.SourceLanguage)} DevSkim: ignore {ruleIds}{Environment.NewLine}";
                            sb.Append(ignoreComment);
                            // CurPos tracks position in the original string,
                            // so we only want to move forward the length of the original deleted content, not the new content
                            currLine = zbEndLine + 1;
                        }

                        foreach (var line in theContent[currLine..])
                            sb.Append($"{line}{Environment.NewLine}");

                        if (!_opts.DryRun)
                        {
                            File.WriteAllText(potentialPath, sb.ToString());
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine($"{potentialPath} specified in sarif does not appear to exist on disk.");
                    }
                }
            }

            return (int)ExitCode.NoIssues;
        }

        private string getInlineComment(string language)
        {
           Languages devSkimLanguages = DevSkimLanguages.LoadEmbedded();
           return devSkimLanguages.GetCommentInline(language);
        }
    }
}