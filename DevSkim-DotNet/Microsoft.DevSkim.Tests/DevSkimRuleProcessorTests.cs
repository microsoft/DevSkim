namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class DevSkimRuleProcessorTests
    {

        [TestMethod]
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
        [DataRow("csharp", "DS123456", 30, DisplayName = "C# with 30 days duration")]
        [DataRow("python", "DS789012", 15, DisplayName = "Python with 15 days duration")]
        [DataRow("sql", "DS111213", 60, DisplayName = "SQL with 60 days duration")]
        [DataRow("vb", "DS141516", 7, DisplayName = "VB with 7 days duration")]
        public void GenerateSuppressionByLanguageTest_WithDuration(string language, string ruleId, int duration)
        {
            // Test suppression with expiration date
            DateTime testDate = DateTime.Now.AddDays(duration);
            string expectedDate = testDate.ToString("yyyy-MM-dd");

            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, ruleId, duration: duration);

            Assert.IsTrue(result.Contains("until"));
            Assert.IsTrue(result.Contains(expectedDate));
            Assert.IsTrue(result.Contains($"ignore {ruleId} until"));
        }

        [TestMethod]
        [DataRow("csharp", "DS123456", "JohnDoe", "// DevSkim: ignore DS123456 by JohnDoe", DisplayName = "C# with reviewer")]
        [DataRow("python", "DS789012", "JaneSmith", "# DevSkim: ignore DS789012 by JaneSmith", DisplayName = "Python with reviewer")]
        [DataRow("sql", "DS111213", "BobJones", "-- DevSkim: ignore DS111213 by BobJones", DisplayName = "SQL with reviewer")]
        [DataRow("vb", "DS141516", "AliceWilliams", "' DevSkim: ignore DS141516 by AliceWilliams", DisplayName = "VB with reviewer")]
        public void GenerateSuppressionByLanguageTest_WithReviewer(string language, string ruleId, string reviewerName, string expected)
        {
            // Test suppression with reviewer name
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, ruleId, reviewerName: reviewerName);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [DataRow("csharp", "DS123456", 15, "JaneSmith", DisplayName = "C# with duration and reviewer")]
        [DataRow("python", "DS789012", 30, "JohnDoe", DisplayName = "Python with duration and reviewer")]
        [DataRow("sql", "DS111213", 7, "BobJones", DisplayName = "SQL with duration and reviewer")]
        [DataRow("vb", "DS141516", 45, "AliceWilliams", DisplayName = "VB with duration and reviewer")]
        public void GenerateSuppressionByLanguageTest_WithDurationAndReviewer(string language, string ruleId, int duration, string reviewerName)
        {
            // Test suppression with both duration and reviewer
            DateTime testDate = DateTime.Now.AddDays(duration);
            string expectedDate = testDate.ToString("yyyy-MM-dd");

            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, ruleId, duration: duration, reviewerName: reviewerName);

            Assert.IsTrue(result.Contains($"until {expectedDate}"));
            Assert.IsTrue(result.Contains($"by {reviewerName}"));
            Assert.IsTrue(result.Contains($"ignore {ruleId} until"));
            Assert.IsTrue(result.EndsWith($" by {reviewerName}"));
        }

        [TestMethod]
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
        [DataRow("xml", "DS123456", "<!-- DevSkim: ignore DS123456 -->", DisplayName = "XML Language")]
        public void GenerateSuppressionByLanguageTest_XMLLanguage(string language, string ruleId, string expected)
        {
            // Test XML-like languages
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, ruleId);

            Console.WriteLine($"{language} suppression result: '{result}'");
            Assert.AreEqual(expected, result);
            Assert.IsNotNull(result);
        }

        [TestMethod]
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
        [DataRow("csharp", "DS123456", "// DevSkim: ignore DS123456", DisplayName = "C# with custom languages")]
        [DataRow("python", "DS789012", "# DevSkim: ignore DS789012", DisplayName = "Python with custom languages")]
        [DataRow("sql", "DS111213", "-- DevSkim: ignore DS111213", DisplayName = "SQL with custom languages")]
        public void GenerateSuppressionByLanguageTest_CustomLanguagesObject(string language, string ruleId, string expected)
        {
            // Test with custom languages object
            var customLanguages = DevSkimLanguages.LoadEmbedded();
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(language, ruleId, languages: customLanguages);

            Assert.AreEqual(expected, result);
        }
    }
}
