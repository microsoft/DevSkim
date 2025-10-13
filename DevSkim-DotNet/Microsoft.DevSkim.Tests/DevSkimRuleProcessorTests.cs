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

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_BasicSuppression()
        {
            // Test basic suppression generation for C# (inline comment style)
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "DS123456");

            Assert.AreEqual("// DevSkim: ignore DS123456", result);
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

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_MultiLinePreferred()
        {
            // Test multiline comment preference for C# 
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "DS123456", preferMultiLine: true);

            Assert.IsTrue(result.StartsWith("/* DevSkim: ignore DS123456"));
            Assert.IsTrue(result.EndsWith(" */"));
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_PythonLanguage()
        {
            // Test Python-style comments
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("python", "DS123456");

            Assert.AreEqual("# DevSkim: ignore DS123456", result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_PythonMultiLine()
        {
            // Test Python multiline (should use prefix/suffix style)
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("python", "DS123456", preferMultiLine: true);

            // Python uses # as both inline and prefix, with \n as suffix for multiline
            Assert.IsTrue(result.StartsWith("# DevSkim: ignore DS123456"));
            Assert.IsTrue(result.EndsWith("\n"));
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_SQLLanguage()
        {
            // Test SQL-style comments
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("sql", "DS123456");

            Assert.AreEqual("-- DevSkim: ignore DS123456", result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_MultipleRuleIds()
        {
            // Test suppression with multiple rule IDs
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "DS123456,DS789012");

            Assert.AreEqual("// DevSkim: ignore DS123456,DS789012", result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_UnsupportedLanguage()
        {
            // Test with a language that doesn't have comment configuration
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("unknownlang", "DS123456");

            // Should return empty string or basic format depending on implementation
            Assert.IsNotNull(result);
            // The method should handle unknown languages gracefully
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_EmptyRuleId()
        {
            // Test with empty rule ID
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("csharp", "");

            Assert.AreEqual("// DevSkim: ignore ", result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_VBLanguage()
        {
            // Test Visual Basic style comments
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("vb", "DS123456");

            Assert.AreEqual("' DevSkim: ignore DS123456", result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_XMLLanguage()
        {
            // Test XML language
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage("xml", "DS123456");

            Console.WriteLine($"XML suppression result: '{result}'");
            Assert.AreEqual("<!-- DevSkim: ignore DS123456 -->", result);

            // This test documents the current behavior for XML
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GenerateSuppressionByLanguageTest_NullLanguage()
        {
            // Test with null language parameter
            string result = DevSkimRuleProcessor.GenerateSuppressionByLanguage(null, "DS123456");

            Assert.IsNotNull(result);
            // Should handle null gracefully
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
