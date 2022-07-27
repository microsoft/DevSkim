using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.DevSkim.AI;

public class DevSkimRuleProcessor : RuleProcessor
{
    public DevSkimRuleProcessor(DevSkimRuleSet ruleSet, DevSkimRuleProcessorOptions processorOptions) : base(ruleSet, processorOptions)
    {
    }
}