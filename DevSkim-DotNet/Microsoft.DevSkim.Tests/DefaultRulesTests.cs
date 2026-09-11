namespace Microsoft.DevSkim.Tests;

[TestClass]
public class DefaultRulesTests
{
    private static string _guidanceDirectory = string.Empty;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        string directory = Directory.GetCurrentDirectory();

        /* Given a directory, like: "C:\src\DevSkim\DevSkim-DotNet\Microsoft.DevSkim.Tests\bin\Debug\net8.0"     - local dev
         * OR                       "/mnt/vss/_work/1/s/DevSkim-DotNet/Microsoft.DevSkim.Tests/bin/Debug/net8.0" - CI for DevSkim CLI
         * we want to find:         "C:\src\DevSkim\guidance"
         * 
         * so we look for DevSkim-DotNet and then go up one more level to find guidance.
         */

        var currentDirInfo = new DirectoryInfo(directory);
        while (currentDirInfo != null && currentDirInfo.Name != "DevSkim-DotNet")
        {
            currentDirInfo = currentDirInfo.Parent;
        }

        currentDirInfo = currentDirInfo?.Parent;

        if (currentDirInfo == null)
        {
            string message = $"Could not find DevSkim-DotNet directory from: {directory}.";
            throw new Exception(message);
        }

        string guidanceDir = Path.Combine(currentDirInfo.FullName, "guidance");
        if (!Directory.Exists(guidanceDir))
        {
            throw new Exception($"Guidance directory {guidanceDir} does not exist.");
        }

        _guidanceDirectory = guidanceDir;
    }

    [TestMethod]
    public void ValidateDefaultRules()
    {
        DevSkimRuleSet devSkimRuleSet = DevSkimRuleSet.GetDefaultRuleSet();
        Assert.AreNotEqual(0, devSkimRuleSet.Count());
        var validator = new DevSkimRuleVerifier(new DevSkimRuleVerifierOptions()
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

        Console.WriteLine("{0} of {1} rules have positive self-tests.", result.DevSkimRuleStatuses.Count(x => x.HasPositiveSelfTests), result.DevSkimRuleStatuses.Count());
        Console.WriteLine("{0} of {1} rules have negative self-tests.", result.DevSkimRuleStatuses.Count(x => x.HasNegativeSelfTests), result.DevSkimRuleStatuses.Count());

        Assert.IsTrue(result.Verified);
        Assert.IsFalse(result.DevSkimRuleStatuses.Any(x => x.Errors.Any()));
    }

    [TestMethod]
    public void RazorLanguagePreservesCSharpAndHtmlCoverage()
    {
        var languages = DevSkimLanguages.LoadEmbedded();
        Assert.IsTrue(languages.FromFileNameOut("view.cshtml", out var cshtml));
        Assert.AreEqual("razor", cshtml.Name);
        Assert.IsTrue(languages.FromFileNameOut("component.razor", out var razor));
        Assert.AreEqual("razor", razor.Name);
        Assert.AreEqual("//", languages.GetCommentInline("razor"));
        Assert.AreEqual("<!--", languages.GetCommentPrefix("razor"));
        Assert.AreEqual("-->", languages.GetCommentSuffix("razor"));

        DevSkimRuleSet ruleSet = DevSkimRuleSet.GetDefaultRuleSet();
        string[] missingRazorCoverage = ruleSet
            .Where(rule => rule.AppliesTo?.Contains("csharp") == true && rule.AppliesTo?.Contains("razor") != true)
            .Select(rule => rule.Id)
            .ToArray();
        Assert.AreEqual(0, missingRazorCoverage.Length, $"C# rules missing Razor coverage: {string.Join(", ", missingRazorCoverage)}");
        CollectionAssert.Contains(ruleSet.Single(rule => rule.Id == "DS610000").AppliesTo?.ToList(), "razor");
    }

        [TestMethod]
        [DataRow("curl --tlsv1.2 https://example.com", 1)]
        [DataRow("curl --tlsv1.3 https://example.com", 0)]
        [DataRow("curl --tlsv1.2 --tlsv1.3 https://example.com", 0)]
        [DataRow("curl --tlsv1.2 https://example.com\ncurl --tlsv1.3 https://example.com", 1)]
        [DataRow("wget --secure-protocol=SSLv3 https://example.com; curl --tlsv1.3 https://example.com", 1)]
        public void BooleanExpressionVerifierMatchesAnalyzer(string content, int expectedFindings)
        {
                string rule = @"[{
    ""name"": ""Boolean TLS condition"",
    ""id"": ""DS440016"",
    ""description"": ""Match guarded curl flags and unguarded wget flags."",
    ""recommendation"": ""Maintain TLS protocol agility."",
    ""severity"": ""ManualReview"",
    ""confidence"": ""high"",
    ""tags"": [ ""Cryptography.Protocol.TLS.Hard-Coded"" ],
    ""patterns"": [
        { ""pattern"": ""--tlsv1"", ""type"": ""substring"", ""scopes"": [ ""code"" ], ""label"": ""curlFlag"" },
        { ""pattern"": ""--secure-protocol=\\S*"", ""type"": ""regex"", ""scopes"": [ ""code"" ], ""label"": ""wgetFlag"" }
    ],
    ""conditions"": [
        {
            ""pattern"": { ""pattern"": ""--tlsv1.3"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
            ""negate_finding"": false,
            ""search_in"": ""same-line"",
            ""label"": ""tls13""
        }
    ],
    ""expression"": ""(curlFlag AND NOT tls13) OR wgetFlag"",
    ""must-match"": [
        ""curl --tlsv1.2 https://example.com"",
        ""wget --secure-protocol=SSLv3 https://example.com; curl --tlsv1.3 https://example.com""
    ],
    ""must-not-match"": [ ""curl --tlsv1.3 https://example.com"" ]
}]";
                DevSkimRuleSet ruleSet = new DevSkimRuleSet();
                ruleSet.AddString(rule, "testRules");
                var verifier = new DevSkimRuleVerifier(new DevSkimRuleVerifierOptions()
                {
                        LanguageSpecs = DevSkimLanguages.LoadEmbedded()
                });
                DevSkimRulesVerificationResult result = verifier.Verify(ruleSet);
                Assert.IsTrue(result.Verified, string.Join(Environment.NewLine, result.Errors.SelectMany(status => status.Errors)));

                var analyzer = new DevSkimRuleProcessor(ruleSet, new DevSkimRuleProcessorOptions()
                {
                    SeverityFilter = ApplicationInspector.RulesEngine.Severity.ManualReview
                });
                Assert.AreEqual(expectedFindings, analyzer.Analyze(content, "test.sh").Count());
        }

            [TestMethod]
            [DataRow("DS440016", "test.sh", "curl --tlsv1.2 https://example.com", 1)]
            [DataRow("DS440016", "test.sh", "curl --tlsv1.3 https://example.com", 0)]
            [DataRow("DS440016", "test.sh", "wget --secure-protocol=SSLv3 https://example.com; curl --tlsv1.3 https://example.com", 1)]
            [DataRow("DS610000", "view.cshtml", "<a href=\"https://example.com\" target=\"_blank\">Open</a>", 1)]
            [DataRow("DS610000", "view.razor", "<!-- <a href=\"https://example.com\" target=\"_blank\">Open</a> -->", 0)]
            [DataRow("DS610000", "view.razor", "// <a href=\"https://example.com\" target=\"_blank\">Open</a>", 0)]
            [DataRow("DS610001", "headers.js", "response.setHeader(\"Set-Cookie\", \"sid=one; Secure\");", 1)]
            [DataRow("DS610001", "headers.js", "response.setHeader(\"Set-Cookie\", \"sid=one; Secure; HttpOnly; SameSite=Lax\");", 0)]
            [DataRow("DS610001", "headers.js", "response.setHeader(\"Set-Cookie\", \"sid=one; Secure\"); response.setHeader(\"Set-Cookie\", \"other=two; HttpOnly; SameSite=Lax\");", 2)]
            [DataRow("DS610001", "headers.js", "response.setHeader(\"Set-Cookie\", \"sid=one; Secure\"); response.setHeader(\"Set-Cookie\", \"other=two; Secure; HttpOnly; SameSite=Lax\");", 1)]
            [DataRow("DS610001", "headers.js", "response.setHeader(\n  \"Set-Cookie\",\n  \"sid=one; Secure; HttpOnly; SameSite=Strict\"\n);", 0)]
            [DataRow("DS610001", "headers.cs", "Response.Headers[\"Set-Cookie\"] = \"sid=one; HttpOnly; SameSite=Lax\";", 1)]
            [DataRow("DS610001", "headers.json", "{\"Set-Cookie\": \"sid=one; Secure; HttpOnly; SameSite=Lax\"}", 0)]
            [DataRow("DS610001", "headers.php", "<?php header('Set-Cookie: sid=one; Secure');", 1)]
            [DataRow("DS610001", "headers.php", "<?php header('Set-Cookie: sid=one; Secure; HttpOnly; SameSite=Lax');", 0)]
            [DataRow("DS610001", "headers.js", "const partial = \"Set-Cookie: sid=one; Secure\";\nconst hardened = \"Set-Cookie: other=two; Secure; HttpOnly; SameSite=Lax\";", 1)]
            [DataRow("DS610001", "headers.js", "// response.setHeader(\"Set-Cookie\", \"sid=one\");", 0)]
            [DataRow("DS610001", "headers.yaml", "Set-Cookie: \"sid=one; Secure\"\nX-Other: value", 1)]
            [DataRow("DS610001", "headers.yaml", "Set-Cookie: \"sid=one; Secure; HttpOnly; SameSite=Lax\"\nX-Other: value", 0)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"max-age=31536000\");", 1)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"includeSubDomains\");", 1)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"max-age=31535999; includeSubDomains\");", 1)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"max-age=31536000; includeSubDomains\");", 0)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"max-age=63072000; includeSubDomains\");", 0)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"max-age=31536000\"); response.setHeader(\"Strict-Transport-Security\", \"includeSubDomains\");", 2)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"max-age=300; includeSubDomains\"); response.setHeader(\"Strict-Transport-Security\", \"max-age=31536000; includeSubDomains\");", 1)]
            [DataRow("DS610002", "headers.cs", "Response.Headers[\"Strict-Transport-Security\"] = \"max-age=31536000; includeSubDomains\";", 0)]
            [DataRow("DS610002", "headers.php", "<?php header('Strict-Transport-Security: max-age=\"31536000\"; includeSubDomains');", 0)]
            [DataRow("DS610002", "headers.php", "<?php header('Strict-Transport-Security: max-age=\"31535999\"; includeSubDomains');", 1)]
            [DataRow("DS610002", "headers.js", "response.setHeader(\"Strict-Transport-Security\", \"max-age=\\\"31536000\\\"; includeSubDomains\");", 0)]
            [DataRow("DS610002", "headers.yaml", "Strict-Transport-Security: \"max-age=31536000; includeSubDomains\"\nX-Other: value", 0)]
            public void BooleanExpressionDefaultRulesScanFixtures(string ruleId, string fileName, string content, int expectedFindings)
            {
                DevSkimRuleSet ruleSet = DevSkimRuleSet.GetDefaultRuleSet().WithIds(new[] { ruleId });
                Assert.AreEqual(1, ruleSet.Count(), $"Rule {ruleId} must be embedded exactly once.");
                var analyzer = new DevSkimRuleProcessor(ruleSet, new DevSkimRuleProcessorOptions()
                {
                    SeverityFilter = ruleSet.Single().Severity
                });
                Assert.AreEqual(expectedFindings, analyzer.Analyze(content, fileName).Count());
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

    public static IEnumerable<object[]> DefaultRules
    {
        get
        {
            DevSkimRuleSet devSkimRuleSet = DevSkimRuleSet.GetDefaultRuleSet();
            foreach (DevSkimRule rule in devSkimRuleSet)
            {
                yield return new object[] { rule };
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(DefaultRules))]
    public void Rule_guidance_file_should_be_specified_and_exist(DevSkimRule rule)
    {
        if (rule.Disabled)
        {
            Assert.Inconclusive("Rule is disabled.");
        }

        if (string.IsNullOrEmpty(rule.RuleInfo))
        {
            Assert.Fail("Rule does not specify guidance file.");
        }

        string guidanceFile = Path.Combine(_guidanceDirectory, rule.RuleInfo);
        Assert.IsTrue(File.Exists(guidanceFile), $"Guidance file {guidanceFile} does not exist.");
    }

    [TestMethod]
    [DynamicData(nameof(DefaultRules))]
    public void Rule_guidance_should_be_complete(DevSkimRule rule)
    {
        if (rule.Disabled)
        {
            Assert.Inconclusive("Rule is disabled.");
        }

        if(string.IsNullOrEmpty(rule.RuleInfo))
        {
            Assert.Inconclusive("Rule does not specify guidance file.");
        }

        string guidanceFile = Path.Combine(_guidanceDirectory, rule.RuleInfo);
        if(!File.Exists(guidanceFile))
        {
            Assert.Inconclusive("Guidance file does not exist");
        }

        string guidance = File.ReadAllText(guidanceFile);
        bool hasContent = !string.IsNullOrEmpty(guidance);
        Assert.IsTrue(hasContent, $"Guidance file {guidanceFile} is empty.");

        if (rule.Id != "DS176209" && // "Suspicious comment" - a TODO comment
            (guidance.Contains("TODO", StringComparison.OrdinalIgnoreCase)
            || guidance.Contains("TO DO", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.Fail($"Guidance file {guidanceFile} contains TODO.");
        }
    }
}