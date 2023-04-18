// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class RuleProcessorTest
    {
        [TestMethod]
        public void BasicPass()
        {
            var rules = RuleSet.FromFile(Path.Combine("rules", "valid", "devskim-rules.json"));
            RuleProcessor processor = new RuleProcessor(rules);
            string testString = " sprintf(";

            // Normal functionality test
            Issue[] issues = processor.Analyze(testString, "c");
            Assert.AreEqual(1, issues.Length);

            // Non existent langugage
            issues = processor.Analyze(testString, "");
            Assert.AreEqual(0, issues.Length, "Match.Success should be false, when no language is passed");
        }
        
        [TestMethod]
        public void IsMatch_FalseTest()
        {
            RuleSet ruleset = RuleSet.FromDirectory(Path.Combine("rules", "valid"), null);
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
            RuleProcessor processor = new RuleProcessor(new RuleSet());

            // Langugage is null
            Issue[] issues = processor.Analyze(null, "");
            Assert.AreEqual(0, issues.Length, "Match.Success should be false");
        }

        [TestMethod]
        public void RuleInfoTest()
        {
            RuleSet ruleset = RuleSet.FromDirectory(Path.Combine("rules", "valid"), null);
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

        [TestMethod]
        public void ConditionOnlyAppliesToOneFinding()
        {
            RuleSet ruleSet = new RuleSet();
            ruleSet.AddRule(new Rule("TestWithin")
            {
                Confidence = Confidence.Medium,
                Severity = Severity.Critical,
                AppliesTo = new List<string> { "csharp" },
                Patterns = new List<SearchPattern>
                {
                    new SearchPattern()
                    {
                        Pattern = "http://[^\\^\\s]+",
                        PatternType = PatternType.Regex
                    }
                },
                Conditions = new List<SearchCondition> 
                { 
                    new SearchCondition() 
                    {
                        NegateFinding = true,
                        Pattern = new SearchPattern()
                        {
                            Pattern = "http://localhost",
                            PatternType = PatternType.Regex
                        },
                        SearchIn = "finding-only"
                    } 
                }
            });
            RuleProcessor processor = new RuleProcessor(ruleSet);
            string testString = 
@"http://localhost
http://contoso.com";

            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(1, issues.Length, "Only one finding should be flagged");
            Issue i = issues[0];
            Assert.AreEqual(2, i.StartLocation.Line);
            Assert.AreEqual(1, i.StartLocation.Column);
        }
    }
}