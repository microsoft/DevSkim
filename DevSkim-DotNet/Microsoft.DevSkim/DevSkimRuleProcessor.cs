using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;

namespace Microsoft.DevSkim
{
    public class DevSkimRuleProcessor
    {
        private readonly RuleProcessor _aiProcessor;
        private Languages _languages => _processorOptions.Languages;
        private readonly DevSkimRuleProcessorOptions _processorOptions;

        public DevSkimRuleProcessor(DevSkimRuleSet ruleSet, DevSkimRuleProcessorOptions processorOptions)
        {
            _aiProcessor = new RuleProcessor(ruleSet, processorOptions);
            _processorOptions = processorOptions;
        }

        public IEnumerable<Issue> Analyze(string text, string fileName)
        {
            List<Issue> resultsList = new List<Issue>();
            if (_languages.FromFileNameOut(fileName, out LanguageInfo info))
            {
                // Create a textcontainer
                TextContainer textContainer = new TextContainer(text, info.Name, _languages);
                // Get AI Issues
                // -1 NumLinesContext disables all sample gathering
                List<MatchRecord> matchRecords = _aiProcessor.AnalyzeFile(textContainer, new FileEntry(fileName, new MemoryStream()),
                    info, null, numLinesContext: -1);
                // Apply suppressions
                foreach (MatchRecord matchRecord in matchRecords)
                {
                    if (matchRecord.Rule is DevSkimRule devSkimRule)
                    {
                        Issue issue = new Issue(Boundary: matchRecord.Boundary,
                        StartLocation: textContainer.GetLocation(matchRecord.Boundary.Index),
                        EndLocation: textContainer.GetLocation(matchRecord.Boundary.Index + matchRecord.Boundary.Length),
                        Rule: devSkimRule);
                        if (_processorOptions.EnableSuppressions)
                        {
                            Suppression supp = new(textContainer, issue.StartLocation.Line);
                            SuppressedIssue? supissue = supp.GetSuppressedIssue(issue.Rule.Id);
                            if (supissue is null)
                            {
                                resultsList.Add(issue);
                            }
                            //Otherwise add the suppression info instead
                            else
                            {
                                issue.IsSuppressionInfo = true;

                                if (!resultsList.Any(x =>
                                        x.Rule.Id == issue.Rule.Id && x.Boundary.Index == issue.Boundary.Index))
                                {
                                    resultsList.Add(issue);
                                }
                            }
                        }
                        else
                        {
                            resultsList.Add(issue);
                        }
                    }
                }
            }

            return resultsList;
        }

        /// <summary>
        ///     Applies given fix on the provided source code line
        /// </summary>
        /// <param name="text"> Source code line </param>
        /// <param name="fixRecord"> Fix record to be applied </param>
        /// <returns> Fixed source code line </returns>
        public static string Fix(string text, CodeFix fixRecord)
        {
            string result = string.Empty;

            if (fixRecord?.FixType is { } fr && fr == FixType.RegexReplace)
            {
                if (fixRecord.Pattern is { })
                {
                    //TODO: Better pattern search and modifiers
                    Regex regex = new Regex(fixRecord.Pattern.Pattern ?? string.Empty);
                    result = regex.Replace(text, fixRecord.Replacement ?? string.Empty);
                }
            }

            return result;
        }
    }
}