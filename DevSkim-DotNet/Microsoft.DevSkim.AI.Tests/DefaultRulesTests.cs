namespace Microsoft.DevSkim.AI.Tests;

[TestClass]
public class DefaultRulesTests
{
    [TestMethod]
    public void ValidateDefaultRules()
    {
        var devSkimRuleSet = AI.DevSkimRuleSet.GetDefaultRuleSet();
        Assert.AreNotEqual(0, devSkimRuleSet.Count());
        var validator = new AI.DevSkimRuleVerifier(new DevSkimRuleVerifierOptions()
        {
            LanguageSpecs = DevSkimLanguages.LoadEmbedded()
        });
        var result = validator.Verify(devSkimRuleSet);
        foreach (var status in result.Errors)
        {
            foreach (var error in status.Errors)
            {
                Console.WriteLine(error);
            }
        }
        Assert.IsTrue(result.Verified);
        Assert.IsFalse(result.DevSkimRuleStatuses.Any(x => x.Errors.Any()));
    }
}