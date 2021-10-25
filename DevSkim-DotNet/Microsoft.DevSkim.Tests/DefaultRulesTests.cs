// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class DefaultRulesTests
    {


        [TestMethod]
        public void VerifyDefaultRules()
        {
            var rules = new RuleSet();
            Assembly assembly = Assembly.GetAssembly(typeof(Boundary));
            string filePath = "Microsoft.DevSkim.Resources.devskim-rules.json";
            Stream resource = assembly?.GetManifestResourceStream(filePath);
            if (resource is Stream)
            {
                using StreamReader file = new StreamReader(resource);
                rules.AddString(file.ReadToEnd(), filePath, null);
            }

            var analyzer = new Analyzer();
            analyzer.SetOperation(new ScopedRegexOperation(analyzer));
            analyzer.SetOperation(new WithinOperation(analyzer));
            Assert.IsFalse(analyzer.EnumerateRuleIssues(rules.GetAllOatRules()).Any());
        }

        static RuleProcessor processor = new RuleProcessor(new RuleSet());

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var rules = new RuleSet();
            Assembly assembly = Assembly.GetAssembly(typeof(Boundary));
            string filePath = "Microsoft.DevSkim.Resources.devskim-rules.json";
            Stream resource = assembly?.GetManifestResourceStream(filePath);
            if (resource is Stream)
            {
                using StreamReader file = new StreamReader(resource);
                rules.AddString(file.ReadToEnd(), filePath, null);
            }
            processor = new RuleProcessor(rules);
        }

        [DataRow("DS440000", new string[] { "SSL_V1", "SSLV1", "SSL_V2", "SSLV2", "SSL_V3", "SSLV3" }, new string[] { "SSL", "SSL_", "SSL_V" }, "csharp")]
        [DataTestMethod]
        public void VerifyRule(string ruleId, IEnumerable<string> mustMatch, IEnumerable<string> mustNotMatch, string language)
        {
            foreach (var stringThatMustMatch in mustMatch)
            {
                Assert.IsTrue(processor.Analyze(stringThatMustMatch, language).Any(x => x.Rule.Id == ruleId), $"{stringThatMustMatch} is supposed to match {ruleId}");
            }
            foreach (var stringThatMustNotMatch in mustNotMatch)
            {
                Assert.IsTrue(!processor.Analyze(stringThatMustNotMatch, language).Any(x => x.Rule.Id == ruleId), $"{stringThatMustNotMatch} is not supposed to match {ruleId}");
            }
        }
    }
}