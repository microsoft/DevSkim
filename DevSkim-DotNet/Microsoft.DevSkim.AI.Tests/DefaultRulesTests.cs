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

    [TestMethod]
    public void DenamespacedRule()
    {
        var content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
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
        var rule = @"[{
    ""name"": ""Source code: Java 17"",
    ""id"": ""CODEJAVA000000"",
    ""description"": ""Java 17 maven configuration"",
    ""applies_to_file_regex"": [
      ""pom.xml""
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
        var deSkimRuleSet = new DevSkimRuleSet();
        deSkimRuleSet.AddString(rule, "testRules");
        var analyzer = new DevSkimRuleProcessor(deSkimRuleSet, new DevSkimRuleProcessorOptions());
        var analysis = analyzer.Analyze(content, "pom.xml");
        Console.WriteLine("breakpoint");
    }
}