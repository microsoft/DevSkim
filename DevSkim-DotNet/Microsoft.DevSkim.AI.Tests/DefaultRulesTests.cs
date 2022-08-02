namespace Microsoft.DevSkim.AI.Tests;

[TestClass]
public class DefaultRulesTests
{
    [TestMethod]
    public void ValidateDefaultRules()
    {
        var devSkimRuleSet = AI.DevSkimRuleSet.GetDefaultRuleSet();
        Assert.AreNotEqual(0, devSkimRuleSet.Count());
        var validator = new AI.DevSkimRuleVerifier(new DevSkimRuleVerifierOptions());
        var result = validator.Verify(devSkimRuleSet);
        Assert.IsTrue(result.Verified);
        Assert.IsFalse(result.DevSkimRuleStatuses.Any(x => x.Errors.Any()));
    }
}