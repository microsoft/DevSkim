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

        /// <summary>
        /// Test that rules with recommendations properly include the recommendation in the SARIF Help field
        /// </summary>
        [TestMethod]
        public void When_rule_has_recommendation_then_sarif_help_text_matches_recommendation()
        {
            // Arrange
            const string expectedRecommendation = "Use a more secure hashing algorithm like SHA-256 instead of MD5.";
            var rule = CreateTestRule("TEST001", "Test Rule", "Test description", expectedRecommendation);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST001");
            
            Assert.IsNotNull(sarifRule);
            Assert.AreEqual(expectedRecommendation, sarifRule!["help"]!["text"]!.ToString());
        }

        /// <summary>
        /// Test that rules without recommendations fall back to description in the SARIF Help field
        /// </summary>
        [TestMethod]
        public void When_rule_has_no_recommendation_then_sarif_help_text_falls_back_to_description()
        {
            // Arrange
            const string expectedDescription = "Test description for fallback";
            var rule = CreateTestRule("TEST002", "Test Rule", expectedDescription, null);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST002");
            
            Assert.IsNotNull(sarifRule);
            Assert.AreEqual(expectedDescription, sarifRule!["help"]!["text"]!.ToString());
        }

        /// <summary>
        /// Test that rules without recommendations but with rule info still include help URI
        /// </summary>
        [TestMethod]
        public void When_rule_has_no_recommendation_but_has_rule_info_then_markdown_includes_help_uri()
        {
            // Arrange
            const string ruleInfo = "DS126858.md";
            var rule = CreateTestRule("TEST004", "Test Rule", "Test description", null, ruleInfo);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST004");
            
            Assert.IsNotNull(sarifRule);
            
            // Should fallback to description in text field
            Assert.AreEqual("Test description", sarifRule!["help"]!["text"]!.ToString());
            
            // Markdown should still include help URI
            var markdown = sarifRule["help"]!["markdown"]!.ToString();
            var helpUri = SarifWriter.CreateHelpUri("DS126858.md");
            var expectedMarkdown = $"Visit [{helpUri}]({helpUri}) for additional guidance on this issue.";
            Assert.AreEqual(expectedMarkdown, markdown);
        }

        /// <summary>
        /// Test that rules with empty recommendation string behave like rules without recommendations
        /// </summary>
        [TestMethod]
        public void When_rule_has_empty_recommendation_then_sarif_help_text_falls_back_to_description()
        {
            // Arrange
            const string expectedDescription = "Test description for empty recommendation";
            var rule = CreateTestRule("TEST005", "Test Rule", expectedDescription, "");
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST005");
            
            Assert.IsNotNull(sarifRule);
            Assert.AreEqual(expectedDescription, sarifRule!["help"]!["text"]!.ToString());
        }

        /// <summary>
        /// Test that rules with whitespace-only recommendation string result in empty help due to SARIF SDK serialization behavior
        /// </summary>
        [TestMethod]
        public void When_rule_has_whitespace_only_recommendation_then_sarif_help_is_empty()
        {
            // Arrange
            const string expectedDescription = "Test description for whitespace recommendation";
            const string whitespaceRecommendation = "   \t\n   ";
            var rule = CreateTestRule("TEST006", "Test Rule", expectedDescription, whitespaceRecommendation);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifJson = writer.ToString();
            var sarifOutput = JObject.Parse(sarifJson);
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
        public void When_rule_has_no_recommendation_and_no_description_then_sarif_help_contains_generic_message()
        {
            // Arrange
            const string ruleInfo = "DS126858.md";
            var rule = CreateTestRule("TEST007", "Test Rule", null, null, ruleInfo);
            var issue = CreateTestIssue(rule, "test.cs", "MD5 hash = MD5.Create();");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST007");
            
            Assert.IsNotNull(sarifRule);
            
            var helpText = sarifRule!["help"]!["text"]!.ToString();
            var expectedHelpText = $"Visit {SarifWriter.CreateHelpUri("DS126858.md")} for guidance on this issue.";
            Assert.AreEqual(expectedHelpText, helpText);
        }

        /// <summary>
        /// Test that multiple rules with different recommendation states are handled correctly
        /// </summary>
        [TestMethod]
        public void When_multiple_rules_have_different_recommendation_states_then_each_is_handled_correctly()
        {
            // Arrange
            var rule1 = CreateTestRule("TEST008", "Rule with recommendation", "Description 1", "Recommendation 1");
            var rule2 = CreateTestRule("TEST009", "Rule without recommendation", "Description 2", null);
            var issue1 = CreateTestIssue(rule1, "test1.cs", "MD5 hash = MD5.Create();");
            var issue2 = CreateTestIssue(rule2, "test2.cs", "SHA1 hash = SHA1.Create();");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue1);
            sarifWriter.WriteIssue(issue2);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            
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
        public void When_rule_has_recommendation_and_rule_info_then_markdown_is_properly_formatted()
        {
            // Arrange
            const string recommendation = "Use bcrypt for password hashing.";
            const string ruleInfo = "DS123456.md";
            var rule = CreateTestRule("TEST010", "Password Hashing", "Weak password hashing", recommendation, ruleInfo);
            var issue = CreateTestIssue(rule, "auth.cs", "MD5.HashData(password);");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST010");
            
            Assert.IsNotNull(sarifRule);
            
            var markdown = sarifRule!["help"]!["markdown"]!.ToString();
            var helpUri = SarifWriter.CreateHelpUri(ruleInfo);
            var expectedMarkdown = $"{recommendation} Visit [{helpUri}]({helpUri}) for additional guidance on this issue.";
            
            Assert.AreEqual(expectedMarkdown, markdown);
        }

        /// <summary>
        /// Test that rule with only RuleInfo (no recommendation) has proper markdown
        /// </summary>
        [TestMethod]
        public void When_rule_has_only_rule_info_then_markdown_contains_help_uri()
        {
            // Arrange
            const string ruleInfo = "DS789012.md";
            var rule = CreateTestRule("TEST011", "Test Rule", "Test description", null, ruleInfo);
            var issue = CreateTestIssue(rule, "test.cs", "some code");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST011");
            
            Assert.IsNotNull(sarifRule);
            
            var markdown = sarifRule!["help"]!["markdown"]!.ToString();
            var helpUri = SarifWriter.CreateHelpUri(ruleInfo);
            var expectedMarkdown = $"Visit [{helpUri}]({helpUri}) for additional guidance on this issue.";
            
            Assert.AreEqual(expectedMarkdown, markdown);
        }

        /// <summary>
        /// Test that rule with no recommendation and no rule info has empty markdown
        /// </summary>
        [TestMethod]
        public void When_rule_has_no_recommendation_and_no_rule_info_then_markdown_is_empty()
        {
            // Arrange
            var rule = CreateTestRule("TEST012", "Test Rule", "Test description", null, null);
            var issue = CreateTestIssue(rule, "test.cs", "some code");

            // Act & Assert
            using var writer = new StringWriter();
            using var sarifWriter = new SarifWriter(writer, null, null);
            
            sarifWriter.WriteIssue(issue);
            sarifWriter.FlushAndClose();

            var sarifOutput = JObject.Parse(writer.ToString());
            var sarifRule = GetRuleFromSarif(sarifOutput, "TEST012");
            
            Assert.IsNotNull(sarifRule);
            
            // When there's no recommendation and no rule info, markdown should be empty or null
            var markdown = sarifRule!["help"]!["markdown"];
            if (markdown != null)
            {
                Assert.AreEqual(string.Empty, markdown.ToString());
            }
            else
            {
                // If the SARIF SDK doesn't serialize empty markdown, that's acceptable too
                Assert.IsNull(markdown);
            }
            
            // But text should still fall back to description
            var helpText = sarifRule["help"]!["text"]!.ToString();
            Assert.AreEqual("Test description", helpText);
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
                Confidence = Confidence.High
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

        private JToken? GetRuleFromSarif(JObject sarifOutput, string ruleId)
        {
            var rules = sarifOutput.SelectTokens("$.runs[*].tool.driver.rules[*]");
            return rules.FirstOrDefault(rule => rule["id"]?.ToString() == ruleId);
        }

        #endregion
    }
}