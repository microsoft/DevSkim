namespace Microsoft.DevSkim.Tests;

[TestClass]
public class DefaultRulesTests
{
    [TestMethod]
    public void ValidateDefaultRules()
    {
        DevSkimRuleSet devSkimRuleSet = DevSkim.DevSkimRuleSet.GetDefaultRuleSet();
        Assert.AreNotEqual(0, devSkimRuleSet.Count());
        DevSkimRuleVerifier validator = new DevSkim.DevSkimRuleVerifier(new DevSkimRuleVerifierOptions()
        {
            LanguageSpecs = DevSkimLanguages.LoadEmbedded()
        });
        DevSkimRulesVerificationResult result = validator.Verify(devSkimRuleSet);
        foreach (ApplicationInspector.RulesEngine.RuleStatus status in result.Errors)
        {
            foreach (string error in status.Errors)
            {
                Console.WriteLine(error);
            }
        }

        Console.WriteLine("{0} of {1} rules have positive self-tests.",result.DevSkimRuleStatuses.Count(x => x.HasPositiveSelfTests),result.DevSkimRuleStatuses.Count());
        Console.WriteLine("{0} of {1} rules have negative self-tests.",result.DevSkimRuleStatuses.Count(x => x.HasNegativeSelfTests),result.DevSkimRuleStatuses.Count());

        Assert.IsTrue(result.Verified);
        Assert.IsFalse(result.DevSkimRuleStatuses.Any(x => x.Errors.Any()));
    }

    [TestMethod]
    public void DenamespacedRule()
    {
        string content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
  <modelVersion>4.0.0</modelVersion>

  <groupId>xxx</groupId>
  <artifactId>xxx</artifactId>
  <version>0.1.0-SNAPSHOT</version>
  <packaging>pom</packaging>

  <name>${project.groupId}:${project.artifactId}</name>
  <description />

  <properties>
    <java.version>17</java.version>
  </properties>

</project>";
        string rule = @"[{
  ""name"": ""Source code: Java 17"",
  ""id"": ""CODEJAVA000000"",
  ""description"": ""Java 17 maven configuration"",
  ""applies_to"": [
    ""xml""
  ],
  ""tags"": [
    ""Code.Java.17""
  ],
  ""severity"": ""critical"",
  ""patterns"": [
    {
      ""pattern"": ""17"",
      ""xpaths"" : [""/*[local-name(.)='project']/*[local-name(.)='properties']/*[local-name(.)='java.version']""],
      ""type"": ""regex"",
      ""scopes"": [
        ""code""
      ],
      ""modifiers"": [
        ""i""
      ],
      ""confidence"": ""high""
    }
  ]
}]";
        DevSkimRuleSet devSkimRuleSet = new DevSkimRuleSet();
        devSkimRuleSet.AddString(rule, "testRules");
        DevSkimRuleProcessor analyzer = new DevSkimRuleProcessor(devSkimRuleSet, new DevSkimRuleProcessorOptions());
        IEnumerable<Issue> analysis = analyzer.Analyze(content, "thing.xml");
        Assert.AreEqual(1, analysis.Count());
    }
}