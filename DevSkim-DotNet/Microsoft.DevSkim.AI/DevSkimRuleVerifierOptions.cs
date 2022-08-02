using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.DevSkim.AI
{
    public class DevSkimRuleVerifierOptions : RulesVerifierOptions
    {
        public DevSkimRuleVerifierOptions()
        {
            DisableRequireUniqueIds = true;
        }
    } 
}