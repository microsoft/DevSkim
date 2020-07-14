using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class SarifWriter : Writer
    {
        public SarifWriter(TextWriter textWriter)
        {
            _results = new List<Result>();
            _rules = new Dictionary<string, CodeAnalysis.Sarif.Rule>();

            this.TextWriter = textWriter;
        }

        public override void FlushAndClose()
        {
            SarifLog sarifLog = new SarifLog();
            sarifLog.Version = SarifVersion.OneZeroZero;
            Run runItem = new Run();
            runItem.Tool = new Tool();

            if (Assembly.GetEntryAssembly() is Assembly entryAssembly)
            {
                runItem.Tool.Name = entryAssembly.GetName()
                                 .Name;

                runItem.Tool.FullName = entryAssembly.GetCustomAttribute<AssemblyProductAttribute>()?
                                                     .Product;

                runItem.Tool.Version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                    .InformationalVersion;

                runItem.Results = _results;
                runItem.Rules = _rules;
                sarifLog.Runs = new List<Run>();
                sarifLog.Runs.Add(runItem);

                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    ContractResolver = SarifContractResolver.Instance,
                    Formatting = Formatting.Indented
                };

                TextWriter.Write(JsonConvert.SerializeObject(sarifLog, settings));
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
            loc.AnalysisTarget = new PhysicalLocation(new Uri(Path.GetFullPath(issue.Filename)),
                                                      null,
                                                      new Region(issue.Issue.StartLocation.Line,
                                                                 issue.Issue.StartLocation.Column,
                                                                 issue.Issue.EndLocation.Line,
                                                                 issue.Issue.EndLocation.Column,
                                                                 issue.Issue.Boundary.Index,
                                                                 issue.Issue.Boundary.Length
                                                       ));
            resultItem.Snippet = issue.TextSample;

            if (issue.Issue.Rule.Fixes != null)
                resultItem.Fixes = GetFixits(issue);

            resultItem.Locations = new List<CodeAnalysis.Sarif.Location>();
            resultItem.Locations.Add(loc);
            _results.Add(resultItem);
        }

        private List<Result> _results;

        private Dictionary<string, CodeAnalysis.Sarif.Rule> _rules;

        private void AddRuleToSarifRule(Rule devskimRule)
        {
            if (!_rules.ContainsKey(devskimRule.Id))
            {
                CodeAnalysis.Sarif.Rule sarifRule = new CodeAnalysis.Sarif.Rule();
                sarifRule.Id = devskimRule.Id;
                sarifRule.Name = devskimRule.Name;
                sarifRule.FullDescription = devskimRule.Description;
                sarifRule.HelpUri = new Uri("https://github.com/Microsoft/DevSkim/blob/main/guidance/" + devskimRule.RuleInfo);

                switch (devskimRule.Severity)
                {
                    case Severity.Critical:
                    case Severity.Important:
                    case Severity.Moderate:
                        sarifRule.DefaultLevel = ResultLevel.Error;
                        break;

                    case Severity.BestPractice:
                        sarifRule.DefaultLevel = ResultLevel.Warning;
                        break;

                    default:
                        sarifRule.DefaultLevel = ResultLevel.Note;
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
                    replacements.Add(new Replacement(issue.Issue.Boundary.Index,
                                                     issue.Issue.Boundary.Length,
                                                     RuleProcessor.Fix(issue.TextSample, fix)
                                                     ));

                    List<FileChange> changes = new List<FileChange>();
                    changes.Add(new FileChange(new Uri(Path.GetFullPath(issue.Filename)), null, replacements));

                    fixes.Add(new Fix(fix.Name, changes));
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
                    resultItem.Level = ResultLevel.Error;
                    break;

                case Severity.BestPractice:
                    resultItem.Level = ResultLevel.Warning;
                    break;

                default:
                    resultItem.Level = ResultLevel.Note;
                    break;
            }

            resultItem.RuleId = rule.Id;
            resultItem.Message = rule.Name;
            foreach (string tag in rule.Tags ?? Array.Empty<string>())
            {
                resultItem.Tags.Add(tag);
            }
        }
    }
}