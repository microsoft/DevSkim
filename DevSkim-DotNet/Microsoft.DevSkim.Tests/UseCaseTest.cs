// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class UseCaseTest
    {
        [TestMethod]
        public void CommentingTest()
        {
            string str = Language.GetCommentInline("python");
            Assert.AreEqual("#", str, "Python comment prefix doesn't match");
            str = Language.GetCommentSuffix("python");
            Assert.AreEqual("\n", str, "Python comment suffix doesn't match");

            str = Language.GetCommentInline("klyngon");
            Assert.AreEqual(string.Empty, str, "Klyngon comment prefix doesn't match");
            str = Language.GetCommentSuffix("klyngon");
            Assert.AreEqual(string.Empty, str, "Klyngon comment suffix doesn't match");
        }

        [TestMethod]
        public void Conditions1Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(false))
            {
                EnableSuppressions = true
            };

            // http test
            string testString = "<h:table xmlns:h=\"http://www.w3.org/TR/html4/\">";
            Issue[] issues = processor.Analyze(testString, "xml");
            Assert.AreEqual(0, issues.Length, "http should NOT be flagged");

            // http test
            testString = "<h:table src=\"http://www.w3.org/TR/html4/\">";
            issues = processor.Analyze(testString, "xml");
            Assert.AreEqual(1, issues.Length, "http should be flagged");
            Assert.AreEqual(1, issues[0].StartLocation.Line, "http location line doesn't match");
            Assert.AreEqual(14, issues[0].Boundary.Index, "http index doesn't match");
            Assert.AreEqual(5, issues[0].Boundary.Length, "http length doesn't match");
            Assert.AreEqual("DS137138", issues[0].Rule.Id, "http rule doesn't match");
        }

        [TestMethod]
        public void Conditions2Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(false))
            {
                EnableSuppressions = true
            };

            // $POST test
            string testString = "require_once($_POST['t']);";
            Issue[] issues = processor.Analyze(testString, "php");
            Assert.AreEqual(1, issues.Length, "$_POST should be flagged");
            Assert.AreEqual(1, issues[0].StartLocation.Line, "$_POST location line doesn't match");
            Assert.AreEqual(0, issues[0].Boundary.Index, "$_POST index doesn't match");
            Assert.AreEqual(19, issues[0].Boundary.Length, "$_POST length doesn't match");
            Assert.AreEqual("DS181731", issues[0].Rule.Id, "$_POST rule doesn't match");

            // $POST test
            testString = "echo(urlencode($_POST['data']);";
            issues = processor.Analyze(testString, "php");
            Assert.AreEqual(0, issues.Length, "$_POST should not be flagged");
        }

        [TestMethod]
        public void LangugeSelectorTest()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(false));
            string testString = "<package id=\"Microsoft.IdentityModel.Tokens\" version=\"5.1.0\"";

            string lang = Language.FromFileName("helloworld.klingon");
            Assert.AreEqual(string.Empty, lang, "Klingon language should not be detected");

            lang = Language.FromFileName(Path.Combine("project", "packages.config"));
            Issue[] issues = processor.Analyze(testString, lang);
            Assert.AreEqual(1, issues.Length, "There should be positive hit");

            bool langExists = Language.GetNames().Contains("csharp");
            Assert.IsTrue(langExists, "csharp should be in the collection");

            langExists = Language.GetNames().Contains("klyngon");
            Assert.IsFalse(langExists, "klingon should not be in the collection");
        }

        public RuleSet LoadRules(bool loadCustomRules)
        {
            RuleSet rules = RuleSet.FromDirectory(Path.Combine("rules", "valid"), null);

            if (loadCustomRules)
                rules.AddDirectory(Path.Combine("rules", "custom"), "my rules");

            return rules;
        }

        [TestMethod]
        public void ScopeTest()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(false))
            {
                EnableSuppressions = true
            };

            // Ignore inline comment
            string testString = "var hash = MD5.Create(); // MD5 is not allowed";
            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(1, issues.Length, "MD5 in inline comment should be ignored");
            Assert.AreEqual(11, issues[0].Boundary.Index, "MD5 inline index is wrong");

            // ignore multinline comment
            testString = " /*\r\nMD5 is not allowed\r\n */ \r\nvar hash = MD5.Create();";
            issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(1, issues.Length, "MD5 in multi line comment should be ignored");
            Assert.AreEqual(42, issues[0].Boundary.Index, "MD5 multi line index is wrong");

            // TODO test
            testString = "//TODO: fix it later";
            processor.SeverityLevel |= Severity.ManualReview;
            issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(1, issues.Length, "TODO should be flagged");
        }

        [TestMethod]
        public void UseCase_EmptyString_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true));
            // Empty string test
            string testString = "";
            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(0, issues.Length, "Empty test failed");
        }

        [TestMethod]
        public void UseCase_IgnoreAllRules_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true))
            {
                EnableSuppressions = true
            };

            // Ignore all until test
            DateTime expirationDate = DateTime.Now.AddDays(5);
            string testString = "encryption=false; MD5 hash  = MD5.Create(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            Issue[] issues = processor.Analyze(string.Format(testString, expirationDate), "csharp");
            Assert.AreEqual(2, issues.Length, "Ignore all should flag two infos");
        }

        [TestMethod]
        public void UseCase_IgnoreAllRulesExpired_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true))
            {
                EnableSuppressions = true
            };

            // Expired all test
            DateTime expirationDate = DateTime.Now;
            string testString = "MD5 hash =  new MD5CryptoServiceProvider(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            Issue[] issues = processor.Analyze(string.Format(testString, expirationDate), "csharp");
            Assert.AreEqual(2, issues.Length, "Expired all should be flagged");
        }

        [TestMethod]
        public void UseCase_IgnoreRules_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true))
            {
                EnableSuppressions = true
            };

            // MD5CryptoServiceProvider test
            string testString = "var a = 10;\nMD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858\nvar b = 20;";

            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(2, issues.Length, "MD5CryptoServiceProvider should be flagged");
            Assert.IsTrue(issues.Any(x => x.Boundary.Index == 27), "MD5CryptoServiceProvider invalid index");
            Assert.IsTrue(issues.Any(x => x.Boundary.Length == 24), "MD5CryptoServiceProvider invalid length ");
            Assert.IsTrue(issues.Any(x => x.StartLocation.Line == 2), "MD5CryptoServiceProvider invalid start location line");
            Assert.IsTrue(issues.Any(x => x.StartLocation.Column == 16), "MD5CryptoServiceProvider invalid start location column");
            Assert.IsTrue(issues.Any(x => x.EndLocation.Line == 2), "MD5CryptoServiceProvider invalid end location line");
            Assert.IsTrue(issues.Any(x => x.EndLocation.Column == 40), "MD5CryptoServiceProvider invalid end location column");
            Assert.IsTrue(issues.Any(x => x.Rule.Id == "DS168931"), "MD5CryptoServiceProvider invalid rule");
            Assert.IsTrue(issues.Count(x => x.IsSuppressionInfo) == 1, "MD5CryptoServiceProvider second issue should be info");
        }

        [TestMethod]
        public void UseCase_IgnoreRulesExpired_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true))
            {
                EnableSuppressions = true
            };

            // Expired until test
            DateTime expirationDate = DateTime.Now;
            string testString = "requests.get('somelink', verify = False) #DevSkim: ignore DS126186 until {0:yyyy}-{0:MM}-{0:dd}";
            Issue[] issues = processor.Analyze(string.Format(testString, expirationDate), "python");
            Assert.AreEqual(1, issues.Length, "Expired until should be flagged");
            Assert.AreEqual(false, issues[0].IsSuppressionInfo, "Expired until issue should NOT be info");
        }

        [TestMethod]
        public void UseCase_IgnoreRulesUntil_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true))
            {
                EnableSuppressions = true
            };

            // Ignore until test
            DateTime expirationDate = DateTime.Now.AddDays(5);
            string testString = "requests.get('somelink', verify = False) #DevSkim: ignore DS126186 until {0:yyyy}-{0:MM}-{0:dd}";
            Issue[] issues = processor.Analyze(string.Format(testString, expirationDate), "python");
            Assert.AreEqual(1, issues.Length, "Ignore until should not be flagged");
            Assert.AreEqual(true, issues[0].IsSuppressionInfo, "Ignore until second issue should be info");
        }

        [TestMethod]
        public void UseCase_IgnoreSuppression_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true))
            {
                EnableSuppressions = false
            };

            // MD5CryptoServiceProvider test
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858";

            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(2, issues.Length, "MD5CryptoServiceProvider should be flagged");
            Assert.AreEqual(0, issues[0].Boundary.Index, "MD5CryptoServiceProvider invalid index");
            Assert.AreEqual(3, issues[0].Boundary.Length, "MD5CryptoServiceProvider invalid length ");
            Assert.AreEqual("DS126858", issues[0].Rule.Id, "MD5CryptoServiceProvider invalid rule");
        }

        [TestMethod]
        public void UseCase_IssueTwice_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true));
            // Same issue twice test
            string testString = "MD5 hash = MD5.Create();";
            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(2, issues.Length, "Same issue should be twice on line");
            Assert.AreEqual(issues[0].Rule, issues[1].Rule, "Same issues should have sames rule IDs");
        }

        [TestMethod]
        public void UseCase_ManualReview_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true));
            // QUICKFIX test
            processor.SeverityLevel |= Severity.ManualReview;
            string testString = "//QUICKFIX: fix this later";
            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(1, issues.Length, "QUICKFIX should be flagged");
            Assert.AreEqual(2, issues[0].Boundary.Index, "QUICKFIX invalid index");
            Assert.AreEqual(8, issues[0].Boundary.Length, "QUICKFIX invalid length ");
            Assert.AreEqual("DS276209", issues[0].Rule.Id, "QUICKFIX invalid rule");
            Assert.AreEqual(0, issues[0].Rule.Fixes.Length, "QUICKFIX invalid Fixes");
            Assert.AreEqual("my rules", issues[0].Rule.RuntimeTag, "QUICKFIX invalid tag");
        }

        [TestMethod]
        public void UseCase_Normal_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true));

            string lang = Language.FromFileName("testfilename.cpp");
            string testString = "strcpy(dest,src);";

            // strcpy test
            Issue[] issues = processor.Analyze(testString, lang);
            Assert.AreEqual(1, issues.Length, "strcpy should be flagged");
            Assert.AreEqual(0, issues[0].Boundary.Index, "strcpy invalid index");
            Assert.AreEqual(16, issues[0].Boundary.Length, "strcpy invalid length ");
            Assert.AreEqual(1, issues[0].StartLocation.Line, "strcpy invalid start location line ");
            Assert.AreEqual(1, issues[0].StartLocation.Column, "strcpy invalid start location column ");
            Assert.AreEqual(1, issues[0].EndLocation.Line, "strcpy invalid end location line ");
            Assert.AreEqual(16, issues[0].EndLocation.Column, "strcpy invalid end location column ");
            Assert.AreEqual("DS185832", issues[0].Rule.Id, "strcpy invalid rule");

            // Fix it test
            Assert.AreNotEqual(issues[0].Rule.Fixes.Length, 0, "strcpy invalid Fixes");
            CodeFix fix = issues[0].Rule.Fixes[0];
            string fixedCode = RuleProcessor.Fix(testString, fix);
            Assert.AreEqual("strcpy_s(dest, <size of dest>, src);", fixedCode, "strcpy invalid code fix");
            Assert.IsTrue(fix.Name.Contains("Change to strcpy_s"), "strcpy wrong fix name");
        }

        [TestMethod]
        public void UseCase_OnError_Test()
        {
            bool error = false;

            RuleSet rules = new RuleSet();
            rules.OnDeserializationErrorEventHandler += delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                error = true;
                args.ErrorContext.Handled = true;
            };

            rules.AddDirectory(Path.Combine("rules", "invalid"), null);
            Assert.IsTrue(error, "Error should be raised");
        }

        [TestMethod]
        public void UseCase_OverlapingIssues_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true));
            // Overlaping issues
            string testString = "            MD5 hash = new MD5CryptoServiceProvider();";
            Issue[] issues = processor.Analyze(testString, "csharp");
            Assert.AreEqual(2, issues.Length, "Overlaping issue count doesn't add up");
        }

        [TestMethod]
        public void UseCase_OverrideTest_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true));
            // Override test
            string testString = "strncat(dest, \"this is also bad\", strlen(dest))";
            Issue[] issues = processor.Analyze(testString, new string[] { "c", "cpp" });
            Assert.AreEqual(2, issues.Length, "Override test failed");
        }

        [TestMethod]
        public void UseCase_SeverityFilter_Test()
        {
            RuleProcessor processor = new RuleProcessor(LoadRules(true));
            string testString = "eval(something)";
            Issue[] issues = processor.Analyze(testString, "javascript");
            Assert.AreEqual(0, issues.Length, "Manual Review should not be flagged");

            processor.SeverityLevel |= Severity.ManualReview;
            issues = processor.Analyze(testString, "javascript");
            Assert.AreEqual(1, issues.Length, "Manual Review should be flagged");
        }

        [TestMethod]
        public void UseCase_SuppressionExists_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until {0:yyyy}-{0:MM}-{0:dd}";
            DateTime expirationDate = DateTime.Now.AddDays(5);

            Suppression sup = new Suppression(string.Format(testString, expirationDate));
            Assert.IsNotNull(sup.GetSuppressedIssue("DS126858"), "Is suppressed DS126858 should be True");
            Assert.IsNotNull(sup.GetSuppressedIssue("DS168931"), "Is suppressed DS168931 should be True");

            Assert.IsTrue(sup.IsInEffect, "Suppression should be in effect");
            Assert.AreEqual(45, sup.Index, "Suppression start index doesn't match");
            Assert.AreEqual(50, sup.Length, "Suppression length doesn't match");
            Assert.AreEqual(expirationDate.ToShortDateString(), sup.ExpirationDate.ToShortDateString(), "Suppression date doesn't match");

            SuppressedIssue[] issues = sup.GetIssues();
            Assert.IsNotNull(issues.FirstOrDefault(x => x.ID == "DS126858"), "Issues list is missing DS126858");
            Assert.IsNotNull(issues.FirstOrDefault(x => x.ID == "DS168931"), "Issues list is missing DS168931");
        }
    }
}