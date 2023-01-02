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
        private readonly Languages devSkimLanguages;

        public SuppressionCommand(SuppressionCommandOptions options)
        {
            _opts = options;
            devSkimLanguages = DevSkimLanguages.LoadEmbedded();
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
                    var issueRecords = resultGroup
                      .Where(x => x.Locations is { })
                      .SelectMany(x => x.Locations, (x, y) => new { x.RuleId, y.PhysicalLocation })
                      .ToList();

                    issueRecords
                    .Sort((a, b) => a.PhysicalLocation.Region.StartLine - b.PhysicalLocation.Region.StartLine);

                    var distinctIssueRecords = issueRecords
                    .GroupBy(x => x.PhysicalLocation.Region.StartLine)
                    .Select(x => new
                    {
                        PhysicalLocation = x.FirstOrDefault().PhysicalLocation,
                        RulesId = string.Join(",", x.Select(y => y.RuleId).Distinct())
                    });

                    if (!File.Exists(potentialPath))
                    {
                        Console.Error.WriteLine($"{potentialPath} specified in sarif does not appear to exist on disk.");
                    }

                    var theContent = File.ReadAllText(potentialPath).Split(Environment.NewLine);
                    int currLine = 0;
                    var sb = new StringBuilder();

                    foreach (var issueRecord in distinctIssueRecords)
                    {
                        var region = issueRecord?.PhysicalLocation.Region;
                        var zbStartLine = region.StartLine - 1;
                        var isMultiline = theContent[zbStartLine].EndsWith(@"\");
                        var ignoreComment = GenerateSuppression(region.SourceLanguage, issueRecord.RulesId, _opts.PreferMultiline || isMultiline, _opts.Duration);

                        foreach (var line in theContent[currLine..zbStartLine])
                        {
                            sb.Append($"{line}{Environment.NewLine}");
                        }

                        var suppressionComment = isMultiline ? $"{ignoreComment}{theContent[zbStartLine]}{Environment.NewLine}" :
                         $"{theContent[zbStartLine]} {ignoreComment}{Environment.NewLine}";
                        sb.Append(suppressionComment);

                        currLine = zbStartLine + 1;
                    }

                    if (currLine < theContent.Length)
                    {
                        foreach (var line in theContent[currLine..^1])
                        {
                            sb.Append($"{line}{Environment.NewLine}");
                        }
                        sb.Append($"{theContent.Last()}");
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
            }

            return (int)ExitCode.NoIssues;
        }

        private string GenerateSuppression(string sourceLanguage, string rulesId, bool preferMultiLine = false, int duration = 0)
        {
            var inline = devSkimLanguages.GetCommentInline(sourceLanguage);
            var expiration = duration > 0 ? DateTime.Now.AddDays(duration).ToString("yyyy-MM-dd") : string.Empty;
            if (!preferMultiLine && !string.IsNullOrEmpty(inline))
            {
                var sb = new StringBuilder();
                sb.Append($"{inline} DevSkim: ignore {rulesId}");
                if (!string.IsNullOrEmpty(expiration))
                {
                    sb.Append($" until {expiration}");
                }
                return sb.ToString();
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append($"{devSkimLanguages.GetCommentPrefix(sourceLanguage)} DevSkim: ignore {rulesId}");
                if (!string.IsNullOrEmpty(expiration))
                {
                    sb.Append($" until {expiration}");
                }

                sb.Append($" {devSkimLanguages.GetCommentSuffix(sourceLanguage)}");
                return sb.ToString();
            }
        }
    }
}