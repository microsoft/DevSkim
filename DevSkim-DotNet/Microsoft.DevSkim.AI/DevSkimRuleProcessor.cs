using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;

namespace Microsoft.DevSkim.AI;

public class DevSkimRuleProcessor
{
    private readonly RuleProcessor _aiProcessor;
    private readonly Languages _languages;

    public DevSkimRuleProcessor(DevSkimRuleSet ruleSet, DevSkimRuleProcessorOptions processorOptions)
    {
        _aiProcessor = new RuleProcessor(ruleSet, processorOptions);
        _languages = new Languages();
    }

    public IEnumerable<Issue> Analyze(string text, string fileName)
    {
        List<Issue> resultsList = new();
        if (_languages.FromFileNameOut(fileName, out LanguageInfo info))
        {                
            // Create a textcontainer
            TextContainer textContainer = new(text, info.Name, _languages);
            // Get AI Issues
            // -1 NumLinesContext disables all sample gathering
            var matchRecords = _aiProcessor.AnalyzeFile(textContainer, new FileEntry(fileName, new MemoryStream()), info, null, numLinesContext: -1);
            // Apply suppressions
            foreach (var matchRecord in matchRecords)
            {
                var issue = new Issue(Boundary: matchRecord.Boundary, StartLocation: textContainer.GetLocation(matchRecord.Boundary.Index), EndLocation: textContainer.GetLocation(matchRecord.Boundary.Index + matchRecord.Boundary.Length), Rule: (DevSkimRule)matchRecord.Rule);
                if (EnableSuppressions)
                {
                    var supp = new Suppression(textContainer, issue.StartLocation.Line);
                    var supissue = supp.GetSuppressedIssue(issue.Rule.Id);
                    if (supissue is null)
                    {
                        resultsList.Add(issue);
                    }
                    //Otherwise add the suppression info instead
                    else
                    {
                        issue.IsSuppressionInfo = true;

                        if (!resultsList.Any(x => x.Rule.Id == issue.Rule.Id && x.Boundary.Index == issue.Boundary.Index))
                            resultsList.Add(issue);
                    }
                }
                else
                {
                    resultsList.Add(issue);
                }
            }
        }

        return resultsList;
    }

    public bool EnableSuppressions { get; set; }
}