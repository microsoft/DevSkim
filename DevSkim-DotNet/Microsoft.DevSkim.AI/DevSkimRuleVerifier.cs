using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.DevSkim.AI;

public class DevSkimRuleVerifier
{
    private RulesVerifier _appInspectorVerifier;
    public DevSkimRuleVerifier(DevSkimRuleVerifierOptions options)
    {
        _appInspectorVerifier = new RulesVerifier(options);
    }
    
    public DevSkimRulesVerificationResult Verify(DevSkimRuleSet ruleSet)
    {
        var devSkimResult = new DevSkimRulesVerificationResult(_appInspectorVerifier.Verify(ruleSet));
        // TODO: Validate devskim specific stuff like fix its
        return devSkimResult;
    }
}