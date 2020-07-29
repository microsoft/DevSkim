using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class SarifWriter : Writer
    {
        public SarifWriter(TextWriter writer, string? outputPath)
        {
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
            if (Assembly.GetEntryAssembly() is Assembly entryAssembly)
            {
                runItem.Tool.Driver.Name = entryAssembly.GetName().Name;

                runItem.Tool.Driver.FullName = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>()?
                                                     .Product;

                runItem.Tool.Driver.Version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                    .InformationalVersion;
            }

            runItem.Tool.Driver.Rules = _rules.Select(x => x.Value).ToList();
            runItem.Results = _results;

            sarifLog.Runs = new List<Run>();
            sarifLog.Runs.Add(runItem);

            if (OutputPath is string)
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

        public override void WriteIssue(IssueRecord issue)
        {
            Result resultItem = new Result();
            MapRuleToResult(issue.Issue.Rule, ref resultItem);
            AddRuleToSarifRule(issue.Issue.Rule);

            CodeAnalysis.Sarif.Location loc = new CodeAnalysis.Sarif.Location();
            loc.PhysicalLocation = new PhysicalLocation()
            {
                Address = new Address() { FullyQualifiedName = Path.GetFullPath(issue.Filename.TrimStart(':')) },
                Region = new Region()
                {
                    StartLine = issue.Issue.StartLocation.Line,
                    StartColumn = issue.Issue.StartLocation.Column,
                    EndLine = issue.Issue.EndLocation.Line,
                    EndColumn = issue.Issue.EndLocation.Column,
                    CharOffset = issue.Issue.Boundary.Index,
                    CharLength = issue.Issue.Boundary.Length,
                    Snippet = new ArtifactContent()
                    {
                        Text = issue.TextSample,
                        Rendered = new MultiformatMessageString(issue.TextSample, $"`{issue.TextSample}`", null),
                    },
                    SourceLanguage = issue.Language
                }
            };

            if (issue.Issue.Rule.Fixes != null)
                resultItem.Fixes = GetFixits(issue);

            resultItem.Locations = new List<CodeAnalysis.Sarif.Location>();
            resultItem.Locations.Add(loc);
            _results.Add(resultItem);
        }

        private List<Result> _results = new List<Result>();

        private Dictionary<string, ReportingDescriptor> _rules = new Dictionary<string, ReportingDescriptor>();

        public string? OutputPath { get; }

        private void AddRuleToSarifRule(Rule devskimRule)
        {
            if (!_rules.ContainsKey(devskimRule.Id))
            {
                ReportingDescriptor sarifRule = new ReportingDescriptor();
                sarifRule.Id = devskimRule.Id;
                sarifRule.Name = devskimRule.Name;
                sarifRule.FullDescription = new MultiformatMessageString() { Text = devskimRule.Description };
                sarifRule.HelpUri = new Uri("https://github.com/Microsoft/DevSkim/blob/main/guidance/" + devskimRule.RuleInfo);
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

                _rules.Add(devskimRule.Id, sarifRule);
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
                    }, new ArtifactContent() { Text = RuleProcessor.Fix(issue.TextSample, fix) }, null));

                    var path = Path.GetFullPath(issue.Filename.TrimStart(':'));
                    var changes = new ArtifactChange[] 
                    {
                        new ArtifactChange(
                            new ArtifactLocation() {
                                Uri = new Uri(path)
                            },
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