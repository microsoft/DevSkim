using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Security.DevSkim;
using System.Diagnostics.CodeAnalysis;

namespace DevSkim.Tests
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
            Match[] matches = processor.Analyze(testString, "csharp");
            Assert.AreEqual(0, matches.Length, "Match.Success should be false");

            // Non existent langugage
            matches = processor.Analyze(testString, "");
            Assert.AreEqual(0, matches.Length, "Match.Success should be false, when no language is passed");
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void IsMatch_InvalidInputTest()
        {
            RuleProcessor processor = new RuleProcessor();

            // Langugage is null
            Match[] matches = processor.Analyze(null, "");
            Assert.AreEqual(0, matches.Length, "Match.Success should be false");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsMatch_InvalidLanguageTest()
        {
            RuleProcessor processor = new RuleProcessor();
            string testString = "this is a test string";

            // Langugage is null
            Match[] matches = processor.Analyze(testString, null);
            Assert.AreEqual(0, matches.Length, "Match.Success should be false");
        }
        
        [TestMethod]
        public void RuleInfoTest()
        {
            Ruleset ruleset = Ruleset.FromDirectory(@"rules\valid", null);
            RuleProcessor processor = new RuleProcessor(ruleset);
            string testString = "strcpy(dest,src);";
            
            Match[] matches = processor.Analyze(testString, "cpp");
            Assert.AreEqual(1, matches.Length, "strcpy should be flagged");

            Rule r = matches[0].Rule;
            Assert.IsTrue(r.Description.Contains("strcpy"), "Invalid decription");
            Assert.IsTrue(r.Source.Contains("dangerous_api.json"), "Invalid file");
            Assert.IsTrue(r.Name.Contains("strcpy"), "Invalid name");
            Assert.IsTrue(r.Replecement.Contains("strcpy_s"), "Invalid replacement");
            Assert.IsTrue(r.RuleInfo.Contains(r.Id), "Invalid ruleinfo");
        }
    }
}
