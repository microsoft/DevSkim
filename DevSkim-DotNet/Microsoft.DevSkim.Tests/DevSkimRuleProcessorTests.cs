namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class DevSkimRuleProcessorTests
    {
        [TestMethod]
        public void GenerateSuppressionByLanguage()
        {
            var languages = DevSkimLanguages.LoadEmbedded();
        }

        [DataTestMethod]
        [DataRow("csharp", "DS123456", "// DevSkim: ignore DS123456", DisplayName = "C# Basic Suppression")]
        [DataRow("python", "DS123456", "# DevSkim: ignore DS123456", DisplayName = "Python Basic Suppression")]
        [DataRow("sql", "DS123456", "-- DevSkim: ignore DS123456", DisplayName = "SQL Basic Suppression")]
        [DataRow("vb", "DS123456", "' DevSkim: ignore DS123456", DisplayName = "VB Basic Suppression")]
        [DataRow("csharp", "DS123456,DS789012", "// DevSkim: ignore DS123456,DS789012", DisplayName = "Multiple Rule IDs")]
        [DataRow("csharp", "", "// DevSkim: ignore ", DisplayName = "Empty Rule ID")]
        public void GenerateSuppressionByLanguageTest_BasicSuppressions(string language, string ruleId, string expected)
        {
            // Test basic suppression generation for various languages
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, ruleId);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_WithDuration()
        {
            // Test suppression with expiration date
            DateTime testDate = DateTime.Now.AddDays(30);
            string expectedDate = testDate.ToString("yyyy-MM-dd");

            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "DS123456", duration: 30);

            Assert.IsTrue(result.Contains("until"));
            Assert.IsTrue(result.Contains(expectedDate));
            Assert.IsTrue(result.StartsWith("// DevSkim: ignore DS123456 until"));
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_WithReviewer()
        {
            // Test suppression with reviewer name
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "DS123456", reviewerName: "JohnDoe");

            Assert.AreEqual("// DevSkim: ignore DS123456 by JohnDoe", result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_WithDurationAndReviewer()
        {
            // Test suppression with both duration and reviewer
            DateTime testDate = DateTime.Now.AddDays(15);
            string expectedDate = testDate.ToString("yyyy-MM-dd");

            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "DS123456", duration: 15, reviewerName: "JaneSmith");

            Assert.IsTrue(result.Contains($"until {expectedDate}"));
            Assert.IsTrue(result.Contains("by JaneSmith"));
            Assert.IsTrue(result.StartsWith("// DevSkim: ignore DS123456 until"));
            Assert.IsTrue(result.EndsWith(" by JaneSmith"));
        }

        [DataTestMethod]
        [DataRow("csharp", "DS123456", "/*", " */", DisplayName = "C# Multiline")]
        [DataRow("python", "DS123456", "#", "\n", DisplayName = "Python Multiline")]
        public void GenerateSuppressionByLanguageTest_MultiLinePreferred(string language, string ruleId, string expectedStart, string expectedEnd)
        {
            // Test multiline comment preference
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, ruleId, preferMultiLine: true);

            Assert.IsTrue(result.StartsWith($"{expectedStart} DevSkim: ignore {ruleId}"));
            Assert.IsTrue(result.EndsWith(expectedEnd));
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_XMLLanguage()
        {
            // Test XML language
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("xml", "DS123456");

            Console.WriteLine($"XML suppression result: '{result}'");
            Assert.AreEqual("<!-- DevSkim: ignore DS123456 -->", result);

            Assert.IsNotNull(result);
        }

        [DataTestMethod]
        [DataRow(null, DisplayName = "Null Language")]
        [DataRow("unknownlang", DisplayName = "Unknown Language")]
        public void GenerateSuppressionByLanguageTest_InvalidLanguages(string language)
        {
            // Test with invalid language parameters
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, "DS123456");

            Assert.IsNotNull(result);
            // Should handle invalid languages gracefully
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_CustomLanguagesObject()
        {
            // Test with custom languages object
            var customLanguages = DevSkimLanguages.LoadEmbedded();
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "DS123456", languages: customLanguages);

            Assert.AreEqual("// DevSkim: ignore DS123456", result);
        }
    }
}
