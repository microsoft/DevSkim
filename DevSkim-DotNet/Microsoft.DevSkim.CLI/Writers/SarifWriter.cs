using System;
using Microsoft.CodeAnalysis.Sarif;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class SarifWriter : Writer
    {
        public SarifWriter(TextWriter writer, string? outputPath, GitInformation? gitInformation)
        {
            _gitInformation = gitInformation;
            TextWriter = writer;
            OutputPath = outputPath;
        }

        public override void FlushAndClose()
        {
            SarifLog sarifLog = new SarifLog();
            sarifLog.Version = SarifVersion.Current;
            Run runItem = new Run();
            runItem.Tool = new Tool();

            runItem.Tool.Driver = new ToolComponent();
            if (Assembly.GetEntryAssembly() is { } entryAssembly)
            {
                runItem.Tool.Driver.Name = entryAssembly.GetName().Name;

                runItem.Tool.Driver.FullName = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>()?
                    .Product;

                runItem.Tool.Driver.Version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion;
            }

            runItem.Tool.Driver.Rules = _rules.Select(x => x.Value).ToList();
            runItem.Results = _results.ToList();
            if (_gitInformation is { })
            {
                runItem.VersionControlProvenance = new List<VersionControlDetails>()
                {
                    new()
                    {
                        Branch = _gitInformation.Branch,
                        RepositoryUri = _gitInformation.RepositoryUri,
                        RevisionId = _gitInformation.CommitHash
                    }
                };
            }

            sarifLog.Runs = new List<Run>();
            sarifLog.Runs.Add(runItem);

            string path = Path.GetTempFileName();
            sarifLog.Save(path);

            // We have to deserialize the sarif using JSON to get the raw data
            //  Levels which were set to warning will not be populated otherwise
            // https://github.com/microsoft/sarif-sdk/issues/2024
            var reReadLog = JObject.Parse(File.ReadAllText(path));
            var resultsWithoutLevels =
                reReadLog.SelectTokens("$.runs[*].results[*]").Where(t => t["level"] == null).ToList();
            foreach (var result in resultsWithoutLevels)
            {
                result["level"] = "warning";
            }

            // Rules which had a default configuration of Warning will also not have the field populated
            var rulesWithoutDefaultConfiguration = reReadLog.SelectTokens("$.runs[*].tool.driver.rules[*]")
                .Where(t => t["defaultConfiguration"] == null).ToList();
            foreach (var rule in rulesWithoutDefaultConfiguration)
            {
                rule["defaultConfiguration"] = new JObject {{ "level", "warning" }};
            }
            
            // Rules with a DefaultConfiguration object, but where that object has no level also should be set
            // This is not currently devskim behavior, but its possible we may add to the bag
            var rulesWithoutDefaultConfigurationLevel = reReadLog.SelectTokens("$.runs[*].tool.driver.rules[*].defaultConfiguration")
                .Where(t => t["level"] == null).ToList();
            foreach (var rule in rulesWithoutDefaultConfigurationLevel)
            {
                rule["level"] = "warning";
            }
            
            if (!string.IsNullOrEmpty(OutputPath))
            {
                using var jsonWriter = new JsonTextWriter(TextWriter);
                reReadLog.WriteTo(jsonWriter);
                TextWriter.Flush();
            }
            else
            {
                // Output to TextWriter (which should be console)
                using var writer = new JsonTextWriter(TextWriter);
                reReadLog.WriteTo(writer);
                writer.Close();
                TextWriter.Flush();
            }
        }

        private ConcurrentDictionary<string, ArtifactLocation> locationCache = new ConcurrentDictionary<string, ArtifactLocation>();
        
        private ArtifactLocation GetValueAndImplicitlyPopulateCache(string path)
        {
            if (locationCache.TryGetValue(path, out ArtifactLocation? value))
            {
                return value;
            }

            // Need to add UriBaseId = "%srcroot%" when not using absolute paths
            ArtifactLocation newVal = new ArtifactLocation() { Uri = new Uri(path,UriKind.Relative) };
            locationCache[path] = newVal;
            return newVal;
        }

        public override void WriteIssue(IssueRecord issue)
        {
            Result resultItem = new Result();
            MapRuleToResult(issue.Issue.Rule, ref resultItem);
            AddRuleToSarifRule(issue.Issue.Rule);

            CodeAnalysis.Sarif.Location loc = new CodeAnalysis.Sarif.Location()
            {
                PhysicalLocation = new PhysicalLocation()
                {
                    ArtifactLocation = GetValueAndImplicitlyPopulateCache(issue.Filename),
                    Region = new Region()
                    {
                        StartColumn = issue.Issue.StartLocation.Column,
                        StartLine = issue.Issue.StartLocation.Line,
                        EndColumn = issue.Issue.EndLocation.Column,
                        EndLine = issue.Issue.EndLocation.Line,
                        CharOffset = issue.Issue.Boundary.Index,
                        CharLength = issue.Issue.Boundary.Length,
                        Snippet = new ArtifactContent()
                        {
                            Text = issue.TextSample,
                            Rendered = new MultiformatMessageString(issue.TextSample, $"`{issue.TextSample}`", null),
                        },
                        SourceLanguage = issue.Language
                    }
                }
            };

            if (issue.Issue.Rule.Fixes != null)
            {
                resultItem.Fixes = GetFixits(issue);
            }

            resultItem.Level = DevSkimLevelToSarifLevel(issue.Issue.Rule.Severity);
            resultItem.Locations = new List<CodeAnalysis.Sarif.Location>
            {
                loc
            };
            resultItem.SetProperty("DevSkimSeverity", issue.Issue.Rule.Severity.ToString());
            _results.Push(resultItem);
        }

        static FailureLevel DevSkimLevelToSarifLevel(Severity severity) => severity switch
        {
            var s when s.HasFlag(Severity.Critical) => FailureLevel.Error,
            var s when s.HasFlag(Severity.Important) => FailureLevel.Error,
            var s when s.HasFlag(Severity.Moderate) => FailureLevel.Warning,
            var s when s.HasFlag(Severity.BestPractice) => FailureLevel.Note,
            var s when s.HasFlag(Severity.ManualReview) => FailureLevel.Note,
            _ => FailureLevel.None
        };

        private ConcurrentStack<Result> _results = new ConcurrentStack<Result>();

        private ConcurrentDictionary<string, ReportingDescriptor> _rules = new ConcurrentDictionary<string, ReportingDescriptor>();
        private readonly GitInformation? _gitInformation;

        public string? OutputPath { get; }

        private void AddRuleToSarifRule(DevSkimRule devskimRule)
        {
            if (!_rules.ContainsKey(devskimRule.Id))
            {
                Uri helpUri = new Uri("https://github.com/Microsoft/DevSkim/blob/main/guidance/" + devskimRule.RuleInfo); ;
                ReportingDescriptor sarifRule = new ReportingDescriptor();
                sarifRule.Id = devskimRule.Id;
                sarifRule.Name = devskimRule.Name;
                sarifRule.ShortDescription = new MultiformatMessageString() { Text = devskimRule.Description };
                sarifRule.FullDescription = new MultiformatMessageString() { Text = devskimRule.Description };
                sarifRule.Help = new MultiformatMessageString()
                {
                    Text = devskimRule.Description,
                    Markdown = $"Visit [{helpUri}]({helpUri}) for guidance on this issue."
                };
                sarifRule.HelpUri = helpUri;
                sarifRule.DefaultConfiguration = new ReportingConfiguration() { Enabled = true };
                sarifRule.DefaultConfiguration.Level = DevSkimLevelToSarifLevel(devskimRule.Severity);

                _rules.TryAdd(devskimRule.Id, sarifRule);
            }
        }

        private List<Fix> GetFixits(IssueRecord issue)
        {
            List<Fix> fixes = new List<Fix>();
            if (issue.Issue.Rule.Fixes != null)
            {
                foreach (CodeFix fix in issue.Issue.Rule.Fixes)
                {
                    List<Replacement> replacements = new List<Replacement>();
                    replacements.Add(new Replacement(new Region()
                    {
                        CharOffset = issue.Issue.Boundary.Index,
                        CharLength = issue.Issue.Boundary.Length,
                    }, new ArtifactContent() { Text = DevSkimRuleProcessor.Fix(issue.TextSample, fix) }, null));

                    ArtifactChange[] changes = new ArtifactChange[] 
                    {
                        new ArtifactChange(
                            GetValueAndImplicitlyPopulateCache(issue.Filename),
                            replacements,
                            null)
                    };

                    fixes.Add(new Fix()
                    {
                        ArtifactChanges = changes,
                        Description = new Message() { Text = issue.Issue.Rule.Description }
                    });
                }
            }
            return fixes;
        }

        private void MapRuleToResult(Rule rule, ref Result resultItem)
        {
            switch (rule.Severity)
            {
                case Severity.Critical:
                case Severity.Important:
                case Severity.Moderate:
                    resultItem.Level = FailureLevel.Error;
                    break;

                case Severity.BestPractice:
                    resultItem.Level = FailureLevel.Warning;
                    break;

                default:
                    resultItem.Level = FailureLevel.Note;
                    break;
            }

            resultItem.RuleId = rule.Id;
            resultItem.Message = new Message() { Text = rule.Name };
            foreach (string tag in rule.Tags ?? Array.Empty<string>())
            {
                resultItem.Tags.Add(tag);
            }
        }
    }
}