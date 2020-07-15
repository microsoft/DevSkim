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

            if (Assembly.GetEntryAssembly() is Assembly entryAssembly)
            {
                runItem.Tool.Driver = new ToolComponent();
                runItem.Tool.Driver.Name = entryAssembly.GetName().Name;

                runItem.Tool.Driver.FullName = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>()?
                                                     .Product;

                runItem.Tool.Driver.Version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                    .InformationalVersion;
                runItem.Tool.Driver.Rules = _rules.Select(x => x.Value).ToList();
                runItem.Results = _results;
            }

            sarifLog.Runs = new List<Run>();
            sarifLog.Runs.Add(runItem);

            if (OutputPath is string)
            {
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
            }

            TextWriter.Flush();
            TextWriter.Close();
        }

        public override void WriteIssue(IssueRecord issue)
        {
            Result resultItem = new Result();
            MapRuleToResult(issue.Issue.Rule, ref resultItem);
            AddRuleToSarifRule(issue.Issue.Rule);

            CodeAnalysis.Sarif.Location loc = new CodeAnalysis.Sarif.Location();
            loc.PhysicalLocation = new PhysicalLocation(new Address() { FullyQualifiedName = Path.GetFullPath(issue.Filename) },
                                                      null,
                                                      new Region(issue.Issue.StartLocation.Line,
                                                                 issue.Issue.StartLocation.Column,
                                                                 issue.Issue.EndLocation.Line,
                                                                 issue.Issue.EndLocation.Column,
                                                                 issue.Issue.Boundary.Index,
                                                                 issue.Issue.Boundary.Length,
                                                                 0, // Byte Offset
                                                                 0, // Byte Length
                                                                 new ArtifactContent(issue.TextSample,
                                                                    null, // "binary"?
                                                                    new MultiformatMessageString(issue.TextSample, $"`{issue.TextSample}`", null), // 
                                                                    null), // Properties
                                                                 null, // Message?
                                                                 issue.Language, // Codelanguage
                                                                 null), // Properties
                                                       null, // Context Region
                                                       null); // Properties
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

                    var changes = new ArtifactChange[] 
                    {
                        new ArtifactChange(
                            new ArtifactLocation() {
                                Uri = new Uri(Path.GetFullPath(issue.Filename))
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