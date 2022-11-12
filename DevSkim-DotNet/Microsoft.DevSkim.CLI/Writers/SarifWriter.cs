using System;
using Microsoft.CodeAnalysis.Sarif;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInspector.RulesEngine;

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

            if (OutputPath != null)
            {
                TextWriter.Close();
                File.Delete(OutputPath);
                sarifLog.Save(OutputPath);
            }
            else
            {
                //Use the text writer
                var path = Path.GetTempFileName();
                sarifLog.Save(path);

                var sr = new StreamReader(path);
                while (!sr.EndOfStream)
                {
                    TextWriter.WriteLine(sr.ReadLine());
                }

                TextWriter.Flush();
                TextWriter.Close();
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
            var newVal = new ArtifactLocation() { Uri = new Uri(path,UriKind.Relative) };
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
                resultItem.Fixes = GetFixits(issue);

            resultItem.Level = DevSkimLevelToSarifLevel(issue.Issue.Rule.Severity);
            resultItem.Locations = new List<CodeAnalysis.Sarif.Location>
            {
                loc
            };
            resultItem.SetProperty<Severity>("DevSkimSeverity", issue.Issue.Rule.Severity);
            _results.Push(resultItem);
        }

        static FailureLevel DevSkimLevelToSarifLevel(Severity severity) => severity switch
        {
            var s when s.HasFlag(Severity.Critical) => FailureLevel.Error,
            var s when s.HasFlag(Severity.Important) => FailureLevel.Warning,
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
                var helpUri = new Uri("https://github.com/Microsoft/DevSkim/blob/main/guidance/" + devskimRule.RuleInfo); ;
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
                switch (devskimRule.Severity)
                {
                    case Severity.Critical:
                    case Severity.Important:
                    case Severity.Moderate:
                        sarifRule.DefaultConfiguration.Level = FailureLevel.Error;
                        break;

                    case Severity.BestPractice:
                        sarifRule.DefaultConfiguration.Level = FailureLevel.Warning;
                        break;

                    default:
                        sarifRule.DefaultConfiguration.Level = FailureLevel.Note;
                        break;
                }

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

                    var changes = new ArtifactChange[] 
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