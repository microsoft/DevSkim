// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Options;
using Location = Microsoft.CodeAnalysis.Sarif.Location;

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
                        groupedResults.Where(x => _opts.FilesToApplyTo.Any(y => x.Key.ToString().Contains(y)));
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
                    var listOfReplacements = resultGroup
                      .Where(x => x.Locations is { })
                      .SelectMany(x => x.Locations, (x, y) => new { x.RuleId, y.PhysicalLocation })
                      .ToList();

                    listOfReplacements
                    .Sort((a, b) => a.PhysicalLocation.Region.StartLine - b.PhysicalLocation.Region.StartLine);

                    var distinctReplacements  = listOfReplacements
                    .GroupBy(x => x.PhysicalLocation.Region.StartLine)
                    .Select(x => new{
                                        PhysicalLocation = x.FirstOrDefault().PhysicalLocation, RulesId = string.Join(",", x.Select(y => y.RuleId))
                                    });

                    if (File.Exists(potentialPath))
                    {
                        var theContent = File.ReadAllText(potentialPath).Split(Environment.NewLine);
                        int currLine = 0;
                        var sb = new StringBuilder();

                        foreach (var replacement in distinctReplacements)
                        {
                            var region = replacement?.PhysicalLocation.Region;
                            var zbStartLine = theContent[0] == string.Empty ? region.StartLine: region.StartLine - 1;
                            var isMultiline = theContent[zbStartLine].EndsWith(@"\");
                            var ignoreComment = $"{getPrefixComment(region.SourceLanguage)} DevSkim: ignore {replacement.RulesId} {getSuffixComment(region.SourceLanguage)}";
                            
                            foreach (var line in theContent[currLine..zbStartLine])
                            {
                                    sb.Append($"{line}{Environment.NewLine}");
                            }
                            
                            var suppressionComment = isMultiline ? $"{ignoreComment}{theContent[zbStartLine]}{Environment.NewLine}" : 
                             $"{theContent[zbStartLine]}{ignoreComment}{Environment.NewLine}";
                            sb.Append(suppressionComment);

                            currLine = zbStartLine + 1;
                        }

                        if (currLine < theContent.Length) 
                        {
                            foreach (var line in theContent[currLine..^1])
                            {
                                sb.Append($"{line}{Environment.NewLine}");
                            }
                            sb.Append($"{potentialPath} will be changed: {File.ReadAllText(potentialPath)}");
                        }

                        if (!_opts.DryRun)
                        {
                            File.WriteAllText(potentialPath, sb.ToString());
                        }
                        else
                        {
                            Console.WriteLine($"{potentialPath} will be changed from: {string.Join(Environment.NewLine, theContent)} to {sb.ToString()}");
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

        private string getSuffixComment(string language)
        {
           Languages devSkimLanguages = DevSkimLanguages.LoadEmbedded();
           return devSkimLanguages.GetCommentSuffix(language);
        }

        private string getPrefixComment(string language)
        {
           Languages devSkimLanguages = DevSkimLanguages.LoadEmbedded();
           return devSkimLanguages.GetCommentPrefix(language);
        }
    }
}