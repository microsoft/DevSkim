// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class RulesetTest
    {
        [TestMethod]
        public void AssignRuleSetTest()
        {
            RuleSet rules = RuleSet.FromDirectory(@"rules\valid", null);
            RuleProcessor proc = new RuleProcessor();
            proc.Rules = rules;

            Assert.AreSame(rules, proc.Rules, "Rulesets needs to match");
        }

        [TestMethod]
        public void AddRuleRangeTest()
        {
            RuleSet rules = RuleSet.FromDirectory(@"rules\valid", null);            

            // Add Range
            RuleSet testRules = new RuleSet();
            testRules.AddRange(rules.ByLanguages(new string[] { "javascript" }));
            Assert.IsTrue(testRules.Count() > 0, "AddRange testRules is empty");

            // Add Rule
            testRules = new RuleSet();
            IEnumerable<Rule> list = rules.ByLanguages(new string[] { "javascript" });
            foreach (Rule r in list)
            {
                testRules.AddRule(r);
            }
            
            Assert.IsTrue(testRules.Count() > 0, "AddRule testRules is empty");
        }

        [TestMethod]
        public void AddRuleFromStringAndFile()
        {
            StreamReader fs = File.OpenText(@"rules\custom\quickfix.json");
            string rule = fs.ReadToEnd();

            // From String
            RuleSet testRules = RuleSet.FromString(rule, "string", null);
            Assert.AreEqual(1, testRules.Count(), "FromString Count should be 1");

            // From File
            testRules.AddFile(@"rules\custom\quickfix.json", null);
            Assert.AreEqual(2, testRules.Count(), "FromFile Count should be 2");

            foreach (Rule r in testRules)
            {
                Assert.IsNotNull(r.Id);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void InvalidRuleFileFailTest()
        {            
            RuleSet ruleset = RuleSet.FromFile("x:\\file.txt", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidRuleFileFailTest2()
        {
            RuleSet ruleset = RuleSet.FromFile(null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void InvalidRuleDirectoryFailTest()
        {
            RuleSet ruleset = RuleSet.FromDirectory("x:\\invalid_directory", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidRuleDirectoryArgsFailTest()
        {
            RuleSet ruleset = RuleSet.FromDirectory(null, null);
        }
    }
}
