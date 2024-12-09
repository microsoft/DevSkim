using System.Text.Json;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim.CLI.Commands;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.Tests;

[TestClass]
public class OptionsTests
{
    [TestMethod]
    public void TestExcludeGlobs()
    {
        var serializedOptsExcludeGlobs = new SerializedAnalyzeCommandOptions()
        {
            Severities = new[] { Severity.Critical | Severity.Important },
            ExitCodeIsNumIssues = true,
            Globs = new List<string>() {"*.js"}
        };
        var testContent = "Hello World";
        var testRule =
@"[
{
    ""name"": ""Weak/Broken Hash Algorithm"",
    ""id"": ""JsonOptionParseTest"",
    ""description"": ""A test that finds hello"",
    ""tags"": [
        ""Tests.JsonOptionsTest""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
        {
            ""pattern"": ""Hello"",
            ""type"": ""regex"",
            ""scopes"": [
                ""code""
            ]
        }
    ]
}]";
        var rulesPath = PathHelper.GetRandomTempFile("json");
        var serializedJsonPath = PathHelper.GetRandomTempFile("json");
        var csharpTestPath = PathHelper.GetRandomTempFile("cs");
        var jsTestPath = PathHelper.GetRandomTempFile("js");
        {
            using var serializedJsonStream = File.Create(serializedJsonPath);
            JsonSerializer.Serialize(serializedJsonStream, serializedOptsExcludeGlobs, new JsonSerializerOptions() { });
            using var csharpStream = File.Create(csharpTestPath);
            JsonSerializer.Serialize(csharpStream, testContent);
            using var jsStream = File.Create(jsTestPath);
            JsonSerializer.Serialize(jsStream, testContent);
            File.WriteAllText(rulesPath, testRule);
        }

        // Create an AnalyzeCommandOptions object referencing our serialized options
        var analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = csharpTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };

        var analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // We set exit code is num issues so this should be 1, as csharp files aren't ignored
        Assert.AreEqual(1, analyzerWithSerialized.Run());
        
        // Create an AnalyzeCommandOptions object referencing our serialized options
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = jsTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };

        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // We set exit code is num issues so this should be 0, as js files are ignored
        Assert.AreEqual(0, analyzerWithSerialized.Run());
    }
    
    [TestMethod]
    public void TestIncludeGlobs()
    {
        var serializedOptsExcludeGlobs = new SerializedAnalyzeCommandOptions()
        {
            Severities = new[] { Severity.Critical | Severity.Important },
            ExitCodeIsNumIssues = true,
            AllowGlobs = new List<string>() {"*.js"}
        };
        var testContent = "Hello World";
        var testRule =
@"[
{
    ""name"": ""Weak/Broken Hash Algorithm"",
    ""id"": ""JsonOptionParseTest"",
    ""description"": ""A test that finds hello"",
    ""tags"": [
        ""Tests.JsonOptionsTest""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
        {
            ""pattern"": ""Hello"",
            ""type"": ""regex"",
            ""scopes"": [
                ""code""
            ]
        }
    ]
}]";
        var rulesPath = PathHelper.GetRandomTempFile("json");
        var serializedJsonPath = PathHelper.GetRandomTempFile("json");
        var csharpTestPath = PathHelper.GetRandomTempFile("cs");
        var jsTestPath = PathHelper.GetRandomTempFile("js");
        {
            using var serializedJsonStream = File.Create(serializedJsonPath);
            JsonSerializer.Serialize(serializedJsonStream, serializedOptsExcludeGlobs, new JsonSerializerOptions() { });
            using var csharpStream = File.Create(csharpTestPath);
            JsonSerializer.Serialize(csharpStream, testContent);
            using var jsStream = File.Create(jsTestPath);
            JsonSerializer.Serialize(jsStream, testContent);
            File.WriteAllText(rulesPath, testRule);
        }

        // Create an AnalyzeCommandOptions object referencing our serialized options
        var analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = csharpTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };

        var analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // We set exit code is num issues so this should be 0, as csharp are implicitly ignored
        Assert.AreEqual(0, analyzerWithSerialized.Run());
        
        // Create an AnalyzeCommandOptions object referencing our serialized options
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = jsTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };

        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // We set exit code is num issues so this should be 1, as js files are included
        Assert.AreEqual(1, analyzerWithSerialized.Run());
    }
    
    [TestMethod]
    public void TestIncludeAndExcludeGlobs()
    {
        var serializedOptsExcludeGlobs = new SerializedAnalyzeCommandOptions()
        {
            Severities = new[] { Severity.Critical | Severity.Important },
            ExitCodeIsNumIssues = true,
            AllowGlobs = new List<string>() {"*.js"},
            Globs = new List<string>() {"*hello.js"}
        };
        var testContent = "Hello World";
        var testRule =
@"[
{
    ""name"": ""Weak/Broken Hash Algorithm"",
    ""id"": ""JsonOptionParseTest"",
    ""description"": ""A test that finds hello"",
    ""tags"": [
        ""Tests.JsonOptionsTest""
    ],
    ""severity"": ""critical"",
    ""patterns"": [
        {
            ""pattern"": ""Hello"",
            ""type"": ""regex"",
            ""scopes"": [
                ""code""
            ]
        }
    ]
}]";
        var rulesPath = PathHelper.GetRandomTempFile("json");
        var serializedJsonPath = PathHelper.GetRandomTempFile("json");
        var helloJsTestPath = PathHelper.GetRandomTempFile("hello.js");
        var jsTestPath = PathHelper.GetRandomTempFile("js");
        {
            using var serializedJsonStream = File.Create(serializedJsonPath);
            JsonSerializer.Serialize(serializedJsonStream, serializedOptsExcludeGlobs, new JsonSerializerOptions() { });
            using var helloJsStream = File.Create(helloJsTestPath);
            JsonSerializer.Serialize(helloJsStream, testContent);
            using var jsStream = File.Create(jsTestPath);
            JsonSerializer.Serialize(jsStream, testContent);
            File.WriteAllText(rulesPath, testRule);
        }

        // Create an AnalyzeCommandOptions object referencing our serialized options
        var analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = helloJsTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };

        var analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // We set exit code is num issues so this should be 0, as hello.js files are ignored
        Assert.AreEqual(0, analyzerWithSerialized.Run());
        
        // Create an AnalyzeCommandOptions object referencing our serialized options
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = jsTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };

        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // We set exit code is num issues so this should be 1, as regular js files are included
        Assert.AreEqual(1, analyzerWithSerialized.Run());
    }
    
    [TestMethod]
    public void TestParsingJsonOptions()
    {
        var ruleIdToIgnore = "JsonOptionParseTest";
        // Create a SerializedAnalyzeCommandOptions object
        var serializedOpts = new SerializedAnalyzeCommandOptions()
        {
            Severities = new[] { Severity.Critical | Severity.Important },
            ExitCodeIsNumIssues = true,
            LanguageRuleIgnoreMap = new Dictionary<string, List<string>>()
            {
                { "csharp", new List<string>() { ruleIdToIgnore } }
            }
        };
        var serializedOpts2 = new SerializedAnalyzeCommandOptions()
        {
            Severities = new[] { Severity.Critical | Severity.Important },
            ExitCodeIsNumIssues = true,
            Globs = new List<string>() {"*.js"}
        };
        // Serialize it to a file
        // Include world twice so we can disinguish between the two rules
        var testContent = "Hello World World";
        var testRule =
@"[
    {
        ""name"": ""Weak/Broken Hash Algorithm"",
        ""id"": ""JsonOptionParseTest"",
        ""description"": ""A test that finds hello"",
        ""tags"": [
            ""Tests.JsonOptionsTest""
        ],
        ""severity"": ""critical"",
        ""patterns"": [
            {
                ""pattern"": ""Hello"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ]
            }
        ]
    },
    {
        ""name"": ""Weak/Broken Hash Algorithm"",
        ""id"": ""JsonOptionParseTest2"",
        ""description"": ""A test that finds hello and isn't ignored"",
        ""tags"": [
            ""Tests.JsonOptionsTest""
        ],
        ""severity"": ""important"",
        ""patterns"": [
            {
                ""pattern"": ""World"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ]
            }
        ]
    }
]";
        var rulesPath = PathHelper.GetRandomTempFile("json");
        var serializedJsonPath = PathHelper.GetRandomTempFile("json");
        var serializedJsonPath2 = PathHelper.GetRandomTempFile("json");
        var csharpTestPath = PathHelper.GetRandomTempFile("cs");
        var jsTestPath = PathHelper.GetRandomTempFile("js");
        {
            using var serializedJsonStream = File.Create(serializedJsonPath);
            JsonSerializer.Serialize(serializedJsonStream, serializedOpts, new JsonSerializerOptions() { });
            using var serializedJsonStream2 = File.Create(serializedJsonPath2);
            JsonSerializer.Serialize(serializedJsonStream2, serializedOpts2, new JsonSerializerOptions() { });
            using var csharpStream = File.Create(csharpTestPath);
            JsonSerializer.Serialize(csharpStream, testContent);
            using var jsStream = File.Create(jsTestPath);
            JsonSerializer.Serialize(jsStream, testContent);
            File.WriteAllText(rulesPath, testRule);
        }

        // Create an AnalyzeCommandOptions object that references the path to the file which ignores a specific rule
        var analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = csharpTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };

        var analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // We set exit code is num issues so this should be 2, from the two matchs for the rule that isn't ignored
        Assert.AreEqual(2, analyzerWithSerialized.Run());
        // Create an AnalyzeCommandOptions object that references the path to the file which ignores a specific rule
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = csharpTestPath,
            Rules = new[] { rulesPath },
            ExitCodeIsNumIssues = true,
            Severities = new Severity[] { Severity.Critical }
        };
        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // This should be 1, because we haven't expressed the json option argument which sets the severity
        Assert.AreEqual(1, analyzerWithSerialized.Run());

        // Try the js which it should find both
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = jsTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath
        };
        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // This should be 3, because no rules are ignored
        Assert.AreEqual(3, analyzerWithSerialized.Run());
        // Try the js which it should find both
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = jsTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath,
            Severities = new[] { Severity.Critical }
        };
        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // This should be 1, because only one rule has severity critical
        Assert.AreEqual(1, analyzerWithSerialized.Run());
        // Test that an option explicitly set overrides an option set in the json
        
        // set of options to test enumerable parsing
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = csharpTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath2
        };
        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // This should be 3, because the globs dont exclude cs files
        Assert.AreEqual(3, analyzerWithSerialized.Run());
        // set of options to test enumerable parsing
        analyzeOpts = new AnalyzeCommandOptions()
        {
            Path = jsTestPath,
            Rules = new[] { rulesPath },
            PathToOptionsJson = serializedJsonPath2
        };
        analyzerWithSerialized = new AnalyzeCommand(analyzeOpts);
        // This should be 0, because the globs exclude js files
        Assert.AreEqual(0, analyzerWithSerialized.Run());
    }
    
    DevSkimRule highConfidenceRule = new DevSkimRule()
    {
        Name = "Weak/Broken Hash Algorithm",
        Id = "HighConfidence",
        Description = "Confidence Filter Tests",
        Tags = new List<string>() { "Tests.ConfidenceFilter" },
        Severity = Severity.Critical,
        Confidence = Confidence.High,
        Patterns = new[]
        {
            new SearchPattern()
            {
                Pattern = "Hello",
                PatternType = PatternType.Regex,
                Scopes = new[]
                {
                    PatternScope.All
                }
            }
        }
    };
    
    DevSkimRule mediumConfidenceRule = new DevSkimRule()
    {
        Name = "Weak/Broken Hash Algorithm",
        Id = "MediumConfidence",
        Description = "Confidence Filter Tests",
        Tags = new List<string>() { "Tests.ConfidenceFilter" },
        Severity = Severity.Critical,
        Confidence = Confidence.Medium,
        Patterns = new[]
        {
            new SearchPattern()
            {
                Pattern = "Hello",
                PatternType = PatternType.Regex,
                Scopes = new[]
                {
                    PatternScope.All
                }
            }
        }
    };
    
    DevSkimRule lowConfidenceRule = new DevSkimRule()
    {
        Name = "Weak/Broken Hash Algorithm",
        Id = "LowConfidence",
        Description = "Confidence Filter Tests",
        Tags = new List<string>() { "Tests.ConfidenceFilter" },
        Severity = Severity.Critical,
        Confidence = Confidence.Low,
        Patterns = new[]
        {
            new SearchPattern()
            {
                Pattern = "Hello",
                PatternType = PatternType.Regex,
                Scopes = new[]
                {
                    PatternScope.All
                }
            }
        }
    };
    
    DevSkimRule unspecifiedConfidenceRule = new DevSkimRule()
    {
        Name = "Weak/Broken Hash Algorithm",
        Id = "UnspecifiedConfidence",
        Description = "Confidence Filter Tests",
        Tags = new List<string>() { "Tests.ConfidenceFilter" },
        Severity = Severity.Critical,
        Confidence = Confidence.Unspecified,
        Patterns = new[]
        {
            new SearchPattern()
            {
                Pattern = "Hello",
                PatternType = PatternType.Regex,
                Scopes = new[]
                {
                    PatternScope.All
                }
            }
        }
    };
    
    [TestMethod]
    public void TestConfidenceFiltering()
    {
        
        var ruleSet = new DevSkimRuleSet();
        ruleSet.AddRule(highConfidenceRule);
        ruleSet.AddRule(mediumConfidenceRule);
        ruleSet.AddRule(lowConfidenceRule);
        ruleSet.AddRule(unspecifiedConfidenceRule);
        Assert.AreEqual(4, ruleSet.Count());
        // Unspecified always passes the confidence filter
        Assert.AreEqual(2, 
            ruleSet.WithConfidenceFilter(Confidence.High)
                .Count());
        Assert.AreEqual(2, 
            ruleSet.WithConfidenceFilter(Confidence.Medium)
                .Count());
        Assert.AreEqual(2, 
            ruleSet.WithConfidenceFilter(Confidence.Low)
                .Count());
        Assert.AreEqual(1, 
            ruleSet.WithConfidenceFilter(Confidence.Unspecified)
                .Count());
    }

    [TestMethod]
    public void TestFullFlowWithConfidenceFiltering()
    {
        var rulesContent = new List<DevSkimRule>()
        {
            unspecifiedConfidenceRule,
            lowConfidenceRule,
            mediumConfidenceRule,
            highConfidenceRule
        };
        var testContent = "Hello";
        var rulesPath = PathHelper.GetRandomTempFile("json");
        var csharpTestPath = PathHelper.GetRandomTempFile("cs");
        {
            using var csharpStream = File.Create(csharpTestPath);
            JsonSerializer.Serialize(csharpStream, testContent);
            File.WriteAllText(rulesPath, JsonSerializer.Serialize(rulesContent));
        }

        var confidenceOptions = new AnalyzeCommandOptions()
        {
            Path = csharpTestPath,
            Rules = new[] { rulesPath },
            Confidences = new [] { Confidence.High, Confidence.Medium, Confidence.Low, Confidence.Unspecified },
            ExitCodeIsNumIssues = true
        };

        var analyzerWithSerialized = new AnalyzeCommand(confidenceOptions);
        Assert.AreEqual(4, analyzerWithSerialized.Run());

        confidenceOptions.Confidences = new[] { Confidence.High };

        // Unspecified confidence rules are not filtered out because confidence may have been
        //  (and should be set) at pattern level
        analyzerWithSerialized = new AnalyzeCommand(confidenceOptions);
        Assert.AreEqual(2, analyzerWithSerialized.Run());
        
        confidenceOptions.Confidences = new[] { Confidence.Medium };
        
        analyzerWithSerialized = new AnalyzeCommand(confidenceOptions);
        Assert.AreEqual(2, analyzerWithSerialized.Run());
        
        confidenceOptions.Confidences = new[] { Confidence.Low };
        
        analyzerWithSerialized = new AnalyzeCommand(confidenceOptions);
        Assert.AreEqual(2, analyzerWithSerialized.Run());

        confidenceOptions.Confidences = new[] { Confidence.Unspecified };

        analyzerWithSerialized = new AnalyzeCommand(confidenceOptions);
        Assert.AreEqual(1, analyzerWithSerialized.Run());
    }
}