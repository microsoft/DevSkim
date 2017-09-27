using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class RuleProcessorTest
    {
        [TestMethod]        
        public void IsMatch_FalseTest()
        {
            Ruleset ruleset = Ruleset.FromDirectory(@"rules\valid", null);
            RuleProcessor processor = new RuleProcessor(ruleset);            
            string testString = "this is a test string";

            // Normal functionality test
            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(0, issues.Length, "Match.Success should be false");

            // Non existent langugage
            issues = processor.Analyze(testString, "");
            Assert.AreEqual(0, issues.Length, "Match.Success should be false, when no language is passed");
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void IsMatch_InvalidInputTest()
        {
            RuleProcessor processor = new RuleProcessor();

            // Langugage is null
            Issue[] issues = processor.Analyze(null, "");
            Assert.AreEqual(0, issues.Length, "Match.Success should be false");
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void IsMatch_InvalidLanguageTest()
        {
            RuleProcessor processor = new RuleProcessor();
            string testString = "this is a test string";

            // Langugage is empty
            Issue[] issues = processor.Analyze(testString, string.Empty);            
        }
        
        [TestMethod]
        public void RuleInfoTest()
        {
            Ruleset ruleset = Ruleset.FromDirectory(@"rules\valid", null);
            RuleProcessor processor = new RuleProcessor(ruleset);
            string testString = "strcpy(dest,src);";
            
            Issue[] issues = processor.Analyze(testString, "cpp");
            Assert.AreEqual(1, issues.Length, "strcpy should be flagged");

            Rule r = issues[0].Rule;
            Assert.IsTrue(r.Description.Contains("strcpy"), "Invalid decription");
            Assert.IsTrue(r.Name.Contains("strcpy"), "Invalid name");
            Assert.IsTrue(r.Recommendation.Contains("strcpy_s"), "Invalid replacement");
            Assert.IsTrue(r.RuleInfo.Contains(r.Id), "Invalid ruleinfo");
        }
    }
}
