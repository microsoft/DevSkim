using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class SarifWriter : Writer
    {
        public SarifWriter()
        {
            _results = new List<Result>();
            _rules = new Dictionary<string, CodeAnalysis.Sarif.Rule>();
        }

        public override void WriteIssue(IssueRecord issue)
        {
            Result resultItem = new Result();
            MapRuleToResult(issue.Issue.Rule, ref resultItem);
            AddRuleToSarifRule(issue.Issue.Rule);

            CodeAnalysis.Sarif.Location loc = new CodeAnalysis.Sarif.Location();
            loc.AnalysisTarget = new PhysicalLocation(new Uri(issue.Filename),
                                                      null,
                                                      new Region(issue.Issue.StartLocation.Line,
                                                                 issue.Issue.StartLocation.Column,
                                                                 issue.Issue.EndLocation.Line,
                                                                 issue.Issue.EndLocation.Column,
                                                                 issue.Issue.Boundary.Index,
                                                                 issue.Issue.Boundary.Length
                                                       ));
            resultItem.Snippet = issue.TextSample;
            resultItem.Fixes = GetFixits(issue);

            resultItem.Locations = new List<CodeAnalysis.Sarif.Location>();
            resultItem.Locations.Add(loc);
            _results.Add(resultItem);            
            
        }

        public override void FlushAndClose()
        {
            SarifLog sarifLog = new SarifLog();
            sarifLog.Version = SarifVersion.OneZeroZero;
            Run runItem = new Run();
            runItem.Tool = new Tool();
            runItem.Tool.FullName = "Microsoft DevSkim CLI";
            runItem.Tool.Name = "DevSkim";
            runItem.Tool.Version = Assembly.GetEntryAssembly()
                                           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
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
            foreach (string tag in rule.Tags)
            {
                resultItem.Tags.Add(tag);
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
                    changes.Add(new FileChange(new Uri(issue.Filename), null, replacements));

                    fixes.Add(new Fix(fix.Name, changes));
                }
            }
            return fixes;
        }

        private void AddRuleToSarifRule(Rule devskimRule)
        {
            if (!_rules.ContainsKey(devskimRule.Id))
            {
                CodeAnalysis.Sarif.Rule sarifRule = new CodeAnalysis.Sarif.Rule();
                sarifRule.Name = devskimRule.Name;
                sarifRule.FullDescription = devskimRule.Description;
                sarifRule.HelpUri = new Uri("https://github.com/Microsoft/DevSkim/blob/master/guidance/" + devskimRule.RuleInfo);

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
        
        private Dictionary<string, CodeAnalysis.Sarif.Rule> _rules;
        private List<Result> _results;
    }
}
