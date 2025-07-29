// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.DevSkim.CLI.Writers;
using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json.Linq;
using AiLocation = Microsoft.ApplicationInspector.RulesEngine.Location;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class SarifWriterTests
    {
        private StringWriter _writer = null!;
        private SarifWriter _sarifWriter = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _writer = new StringWriter();
            _sarifWriter = new SarifWriter(_writer, null, null);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _writer?.Dispose();
        }

        /// <summary>
        /// Test that rules with recommendations properly include the recommendation in the SARIF Help field
        /// </summary>
        [TestMethod]
        public void SarifWriter_RuleWithRecommendation_IncludesRecommendationInHelp()
        {
            // Arrange
            const string expectedRecommendation = "Use a more secure hashing algorithm like SHA-256 instead of MD5.";
            var rule = CreateTestRule("TEST001", "Test Rule", "Test description", expectedRecommendation);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST001");
            
            Assert.IsNotNull(sarifRule);
            Assert.AreEqual(expectedRecommendation, sarifRule!["help"]!["text"]!.ToString());
        }

        /// <summary>
        /// Test that rules without recommendations fall back to description in the SARIF Help field
        /// </summary>
        [TestMethod]
        public void SarifWriter_RuleWithoutRecommendation_FallsBackToDescriptionInHelp()
        {
            // Arrange
            const string expectedDescription = "Test description for fallback";
            var rule = CreateTestRule("TEST002", "Test Rule", expectedDescription, null);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST002");
            
            Assert.IsNotNull(sarifRule);
            Assert.AreEqual(expectedDescription, sarifRule!["help"]!["text"]!.ToString());
        }

        /// <summary>
        /// Test that rules with recommendations and rule info include both in markdown help
        /// </summary>
        [TestMethod]
        public void SarifWriter_RuleWithRecommendationAndRuleInfo_IncludesBothInMarkdown()
        {
            // Arrange
            const string expectedRecommendation = "Use SHA-256 instead of MD5.";
            const string ruleInfo = "DS126858.md";
            var rule = CreateTestRule("TEST003", "Test Rule", "Test description", expectedRecommendation, ruleInfo);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST003");
            
            Assert.IsNotNull(sarifRule);
            
            // Check text field has recommendation
            Assert.AreEqual(expectedRecommendation, sarifRule!["help"]!["text"]!.ToString());
            
            // Check markdown field includes both recommendation and help URI
            var markdown = sarifRule["help"]!["markdown"]!.ToString();
            Assert.IsTrue(markdown.Contains(expectedRecommendation));
            Assert.IsTrue(markdown.Contains("Visit [https://github.com/Microsoft/DevSkim/blob/main/guidance/DS126858.md]"));
            Assert.IsTrue(markdown.Contains("for additional guidance on this issue"));
        }

        /// <summary>
        /// Test that rules without recommendations but with rule info still include help URI
        /// </summary>
        [TestMethod]
        public void SarifWriter_RuleWithoutRecommendationButWithRuleInfo_IncludesHelpUri()
        {
            // Arrange
            const string ruleInfo = "DS126858.md";
            var rule = CreateTestRule("TEST004", "Test Rule", "Test description", null, ruleInfo);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST004");
            
            Assert.IsNotNull(sarifRule);
            
            // Should fallback to description in text field
            Assert.AreEqual("Test description", sarifRule!["help"]!["text"]!.ToString());
            
            // Markdown should still include help URI
            var markdown = sarifRule["help"]!["markdown"]!.ToString();
            Assert.IsTrue(markdown.Contains("Visit [https://github.com/Microsoft/DevSkim/blob/main/guidance/DS126858.md]"));
        }

        /// <summary>
        /// Test that rules with empty recommendation string behave like rules without recommendations
        /// </summary>
        [TestMethod]
        public void SarifWriter_RuleWithEmptyRecommendation_FallsBackToDescription()
        {
            // Arrange
            const string expectedDescription = "Test description for empty recommendation";
            var rule = CreateTestRule("TEST005", "Test Rule", expectedDescription, "");
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST005");
            
            Assert.IsNotNull(sarifRule);
            Assert.AreEqual(expectedDescription, sarifRule!["help"]!["text"]!.ToString());
        }

        /// <summary>
        /// Test that rules with whitespace-only recommendation string result in empty help due to SARIF SDK serialization behavior
        /// </summary>
        [TestMethod]
        public void SarifWriter_RuleWithWhitespaceRecommendation_ResultsInEmptyHelp()
        {
            // Arrange
            const string expectedDescription = "Test description for whitespace recommendation";
            const string whitespaceRecommendation = "   \t\n   ";
            var rule = CreateTestRule("TEST006", "Test Rule", expectedDescription, whitespaceRecommendation);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifJson = _writer.ToString();
            var sarifOutput = ParseSarifOutput(sarifJson);
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST006");
            
            Assert.IsNotNull(sarifRule, $"sarifRule should not be null. SARIF output: {sarifJson}");
            
            // The SARIF SDK appears to serialize whitespace-only text as empty objects
            // So the help field exists but has no text/markdown properties
            var help = sarifRule!["help"];
            Assert.IsNotNull(help, "Help object should exist");
            
            // The text and markdown properties should be null/missing when they contain only whitespace
            var helpText = help["text"];
            var helpMarkdown = help["markdown"];
            
            Assert.IsNull(helpText, "Help text should be null for whitespace-only recommendation");
            Assert.IsNull(helpMarkdown, "Help markdown should be null for whitespace-only recommendation");
        }

        /// <summary>
        /// Test that rules without recommendation and without description fall back to generic help message
        /// </summary>
        [TestMethod]
        public void SarifWriter_RuleWithoutRecommendationAndDescription_FallsBackToGenericHelp()
        {
            // Arrange
            const string ruleInfo = "DS126858.md";
            var rule = CreateTestRule("TEST007", "Test Rule", null, null, ruleInfo);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST007");
            
            Assert.IsNotNull(sarifRule);
            
            var helpText = sarifRule!["help"]!["text"]!.ToString();
            Assert.IsTrue(helpText.Contains("Visit https://github.com/Microsoft/DevSkim/blob/main/guidance/DS126858.md for guidance on this issue"));
        }

        /// <summary>
        /// Test that multiple rules with different recommendation states are handled correctly
        /// </summary>
        [TestMethod]
        public void SarifWriter_MultipleRulesWithDifferentRecommendations_HandledCorrectly()
        {
            // Arrange
            var rule1 = CreateTestRule("TEST008", "Rule with recommendation", "Description 1", "Recommendation 1");
            var rule2 = CreateTestRule("TEST009", "Rule without recommendation", "Description 2", null);
            var issue1 = CreateTestIssue(rule1, "test1.cs", "MD5 hash = MD5.Create();");
            var issue2 = CreateTestIssue(rule2, "test2.cs", "SHA1 hash = SHA1.Create();");

            // Act
            _sarifWriter.WriteIssue(issue1);
            _sarifWriter.WriteIssue(issue2);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            
            var sarifRule1 = GetRuleFromSarif(sarifOutput, "TEST008");
            var sarifRule2 = GetRuleFromSarif(sarifOutput, "TEST009");
            
            Assert.IsNotNull(sarifRule1);
            Assert.IsNotNull(sarifRule2);
            
            // Rule 1 should have recommendation in help text
            Assert.AreEqual("Recommendation 1", sarifRule1!["help"]!["text"]!.ToString());
            
            // Rule 2 should fall back to description in help text
            Assert.AreEqual("Description 2", sarifRule2!["help"]!["text"]!.ToString());
        }

        /// <summary>
        /// Test that markdown field is properly formatted when recommendation is present
        /// </summary>
        [TestMethod]
        public void SarifWriter_RecommendationPresent_MarkdownFieldProperlyFormatted()
        {
            // Arrange
            const string recommendation = "Use bcrypt for password hashing.";
            const string ruleInfo = "DS123456.md";
            var rule = CreateTestRule("TEST010", "Password Hashing", "Weak password hashing", recommendation, ruleInfo);
            var issue = CreateTestIssue(rule, "auth.cs", "MD5.HashData(password);");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST010");
            
            Assert.IsNotNull(sarifRule);
            
            var markdown = sarifRule!["help"]!["markdown"]!.ToString();
            var expectedMarkdown = $"{recommendation} Visit [https://github.com/Microsoft/DevSkim/blob/main/guidance/{ruleInfo}](https://github.com/Microsoft/DevSkim/blob/main/guidance/{ruleInfo}) for additional guidance on this issue.";
            
            Assert.AreEqual(expectedMarkdown, markdown);
        }

        /// <summary>
        /// Test that rule with only RuleInfo (no recommendation) has proper markdown
        /// </summary>
        [TestMethod]
        public void SarifWriter_OnlyRuleInfo_MarkdownContainsHelpUri()
        {
            // Arrange
            const string ruleInfo = "DS789012.md";
            var rule = CreateTestRule("TEST011", "Test Rule", "Test description", null, ruleInfo);
            var issue = CreateTestIssue(rule, "test.cs", "some code");

            // Act
            _sarifWriter.WriteIssue(issue);
            _sarifWriter.FlushAndClose();

            // Assert
            var sarifOutput = ParseSarifOutput(_writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST011");
            
            Assert.IsNotNull(sarifRule);
            
            var markdown = sarifRule!["help"]!["markdown"]!.ToString();
            var expectedMarkdown = $"Visit [https://github.com/Microsoft/DevSkim/blob/main/guidance/{ruleInfo}](https://github.com/Microsoft/DevSkim/blob/main/guidance/{ruleInfo}) for additional guidance on this issue.";
            
            Assert.AreEqual(expectedMarkdown, markdown);
        }

        #region Helper Methods

        private DevSkimRule CreateTestRule(string id, string name, string? description, string? recommendation, string? ruleInfo = null)
        {
            return new DevSkimRule
            {
                Id = id,
                Name = name,
                Description = description ?? string.Empty,
                Recommendation = recommendation,
                RuleInfo = ruleInfo,
                Severity = Severity.Important,
                Confidence = Confidence.High,
                Patterns = new[]
                {
                    new SearchPattern
                    {
                        Pattern = "MD5",
                        PatternType = PatternType.Regex,
                        Scopes = new[] { PatternScope.All }
                    }
                }
            };
        }

        private IssueRecord CreateTestIssue(DevSkimRule rule, string filename, string textSample)
        {
            var boundary = new Boundary
            {
                Index = 0,
                Length = textSample.Length
            };

            var issue = new Issue(
                Boundary: boundary,
                StartLocation: new AiLocation { Line = 1, Column = 1 },
                EndLocation: new AiLocation { Line = 1, Column = textSample.Length + 1 },
                Rule: rule
            );

            return new IssueRecord(filename, 0, textSample, issue, "csharp", null);
        }

        private JObject ParseSarifOutput(string sarifJson)
        {
            return JObject.Parse(sarifJson);
        }

        private JToken? GetRuleFromSarif(JObject sarifOutput, string ruleId)
        {
            var rules = sarifOutput.SelectTokens("$.runs[*].tool.driver.rules[*]");
            return rules.FirstOrDefault(rule => rule["id"]?.ToString() == ruleId);
        }

        #endregion
    }
}