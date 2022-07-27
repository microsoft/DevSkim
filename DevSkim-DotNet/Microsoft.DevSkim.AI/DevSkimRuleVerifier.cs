using Microsoft.ApplicationInspector.Commands;

namespace Microsoft.DevSkim.AI;

public class DevSkimRuleVerifier
{
    private RulesVerifier _appInspectorVerifier;
    public DevSkimRuleVerifier(DevSkimRuleVerifierOptions options)
    {
        _appInspectorVerifier = new RulesVerifier(options);
    }
    
    // Add a method to verify DevSkim rules

    public DevSkimRulesVerificationResult Verify(DevSkimRuleSet ruleSet)
    {
        var aiResult = _appInspectorVerifier.Verify(ruleSet);
        
    }
}