using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Security.DevSkim;
using System.Diagnostics.CodeAnalysis;

namespace DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class UseCaseTest
    {
        [TestMethod]
        public void UseCase_Normal_Test()
        {
            Ruleset rules = Ruleset.FromDirectory(@"rules\valid", null);
            rules.AddDirectory(@"rules\custom", "my rules");

            RuleProcessor processor = new RuleProcessor(rules);
            
            string lang = Language.FromFilename("testfilename.cpp");
            string testString = "strcpy(dest,src);";

            // strcpy test
            Match match = processor.IsMatch(testString, 0, lang);
            Assert.IsTrue(match.Success, "strcpy should be flagged");
            Assert.AreEqual(0, match.Location, "strcpy invalid index");
            Assert.AreEqual(16, match.Length, "strcpy invalid length ");
            Assert.AreEqual("DS185832", match.Rule.Id, "strcpy invalid rule");

            // Fix it test
            Assert.AreNotEqual(match.Rule.Fixes.Length, 0, "strcpy invalid Fixes");
            CodeFix fix = match.Rule.Fixes[0];
            string fixedCode = RuleProcessor.Fix(testString, fix);
            Assert.AreEqual("strcpy_s(dest, <size of dest>, src);", fixedCode, "strcpy invalid code fix");
            Assert.IsTrue(fix.Name.Contains("Change to strcpy_s"), "strcpy wrong fix name" );

            // TODO test
            testString = "//TODO: fix this later";
            match = processor.IsMatch(testString, 0, "csharp");
            Assert.IsTrue(match.Success, "todo should be flagged");
            Assert.AreEqual(2, match.Location, "todo invalid index");
            Assert.AreEqual(4, match.Length, "todo invalid length ");
            Assert.AreEqual("DS176209", match.Rule.Id, "todo invalid rule");
            Assert.AreEqual(0, match.Rule.Fixes.Length, "todo invalid Fixes");
            Assert.AreEqual("my rules", match.Rule.Tag, "todo invalid tag");
        }

        [TestMethod]
        public void UseCase_IgnoreRules_Test()
        {
            Ruleset rules = Ruleset.FromDirectory(@"rules\valid", null);
            rules.AddDirectory(@"rules\custom", null);

            RuleProcessor processor = new RuleProcessor(rules);

            // MD5CryptoServiceProvider test
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858";
            Match match = processor.IsMatch(testString, 0, "csharp");
            Assert.IsTrue(match.Success, "MD5CryptoServiceProvider should be flagged");
            Assert.AreEqual(15, match.Location, "MD5CryptoServiceProvider invalid index");
            Assert.AreEqual(24, match.Length, "MD5CryptoServiceProvider invalid length ");
            Assert.AreEqual("DS168931", match.Rule.Id, "MD5CryptoServiceProvider invalid rule");

            // Ignore until test
            DateTime expirationDate = DateTime.Now.AddDays(5);
            testString = "requests.get('somelink', verify = False) #DevSkim: ignore DS130821 until {0:yyyy}-{0:MM}-{0:dd}";
            match = processor.IsMatch(string.Format(testString, expirationDate), 0, "python");
            Assert.IsFalse(match.Success, "Ignore until should not be flagged");

            // Expired until test
            expirationDate = DateTime.Now;
            match = processor.IsMatch(string.Format(testString, expirationDate), 0, "python");
            Assert.IsTrue(match.Success, "Expired until should be flagged");

            // Ignore all until test
            expirationDate = DateTime.Now.AddDays(5);
            testString = "MD5 hash  = new MD5.Create(); #DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            match = processor.IsMatch(string.Format(testString, expirationDate), 0, "csharp");
            Assert.IsFalse(match.Success, "Ignore all until should not be flagged");

            // Expired all test
            expirationDate = DateTime.Now;
            testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            match = processor.IsMatch(string.Format(testString, expirationDate), 0, "csharp");
            Assert.IsTrue(match.Success, "Expired all should be flagged");
        }

        [TestMethod]
        public void UseCase_Suppress_Test()
        {
            Ruleset rules = Ruleset.FromDirectory(@"rules\valid", null);
            rules.AddDirectory(@"rules\custom", null);

            RuleProcessor processor = new RuleProcessor(rules);

            // Is supressed test
            string testString = "md5.new()";
            Match match = processor.IsMatch(testString, 0, "python");
            Assert.IsTrue(match.Success, "Is suppressed should ve flagged");

            string ruleId = match.Rule.Id;
            Suppressor sup = new Suppressor(testString, "python");
            Assert.IsFalse(sup.IsRuleSuppressed(ruleId), "Is suppressed should be false");

            // Suppress Rule test
            string suppressedString = sup.SuppressRule(ruleId);
            string expected = "md5.new() #DevSkim: ignore DS196098";
            Assert.AreEqual(expected, suppressedString, "Supress Rule failed ");

            // Suppress Rule Until test
            DateTime expirationDate = DateTime.Now.AddDays(5);
            suppressedString = sup.SuppressRule(ruleId, expirationDate);
            expected = string.Format("md5.new() #DevSkim: ignore DS196098 until {0:yyyy}-{0:MM}-{0:dd}", expirationDate);
            Assert.AreEqual(expected, suppressedString, "Supress Rule Until failed ");

            // Suppress All test
            suppressedString = sup.SuppressAll();
            expected = "md5.new() #DevSkim: ignore all";
            Assert.AreEqual(expected, suppressedString, "Supress All failed");

            // Suppress All Until test            
            suppressedString = sup.SuppressAll(expirationDate);
            expected = string.Format("md5.new() #DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}", expirationDate);
            Assert.AreEqual(expected, suppressedString, "Supress All Until failed ");
        }

        [TestMethod]
        public void UseCase_SuppressExisting_Test()
        {
            Ruleset rules = Ruleset.FromDirectory(@"rules\valid", null);
            rules.AddDirectory(@"rules\custom", null);

            RuleProcessor processor = new RuleProcessor(rules);

            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until {0:yyyy}-{0:MM}-{0:dd}";
            DateTime expirationDate = DateTime.Now.AddDays(5);

            Suppressor sup = new Suppressor(string.Format(testString, expirationDate), "csharp");
            Assert.IsTrue(sup.IsRuleSuppressed("DS126858"), "Is suppressed DS126858 should be True");
            Assert.IsTrue(sup.IsRuleSuppressed("DS168931"), "Is suppressed DS168931 should be True");

            // Suppress multiple
            string suppressedString = sup.SuppressRule("DS196098");
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected,expirationDate), suppressedString, "Suppress multiple failed");

            // Suppress multiple to all
            suppressedString = sup.SuppressAll();
            expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all failed");

            // Suppress multiple new date
            expirationDate = DateTime.Now.AddDays(10);
            suppressedString = sup.SuppressRule("DS196098", expirationDate);
            expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple new date failed");

            // Suppress multiple to all new date
            suppressedString = sup.SuppressAll(DateTime.Now.AddDays(10));
            expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all new date failed");
        }

        [TestMethod]
        public void UseCase_SuppressExistingPast_Test()
        {
            Ruleset rules = Ruleset.FromDirectory(@"rules\valid", null);
            rules.AddDirectory(@"rules\custom", null);

            RuleProcessor processor = new RuleProcessor(rules);

            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until 1980-07-15";
            Suppressor sup = new Suppressor(testString, "csharp");            
            Assert.IsFalse(sup.IsRuleSuppressed("DS126858"), "Is suppressed DS126858 should be True");
            Assert.IsFalse(sup.IsRuleSuppressed("DS168931"), "Is suppressed DS168931 should be True");

            // Suppress multiple
            string suppressedString = sup.SuppressRule("DS196098");
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until 1980-07-15";
            Assert.AreEqual(expected, suppressedString, "Suppress multiple failed");

            // Suppress multiple new date            
            DateTime expirationDate = DateTime.Now.AddDays(10);
            suppressedString = sup.SuppressRule("DS196098", expirationDate); 
            expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple new date failed");

            // Suppress multiple to all
            suppressedString = sup.SuppressAll();
            expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until 1980-07-15";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all failed");

            // Suppress multiple to all new date
            suppressedString = sup.SuppressAll(expirationDate);
            expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all new date failed");

        }
    }
}
