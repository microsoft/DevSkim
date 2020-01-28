using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.DevSkim.VSExtension.Tests
{
    [TestClass]
    public class SuppressorExTest
    {
        [TestMethod]
        public void IsSuppress_Test()
        {
            // Is supressed test
            string testString = "md5.new()";
            SuppressionEx sup = new SuppressionEx(testString, "python");
            Assert.IsTrue(sup.Index < 0, "Suppression should not be flagged");
        }

        [TestMethod]
        public void SuppressRule_Test()
        {
            // Suppress Rule test
            string testString = "md5.new()";
            SuppressionEx sup = new SuppressionEx(testString, "python");
            string ruleId = "DS196098";
            string suppressedString = sup.SuppressIssue(ruleId);
            string expected = "md5.new() #DevSkim: ignore DS196098\n";
            Assert.AreEqual(expected, suppressedString, "Supress Rule failed ");
        }

        [TestMethod]
        public void SuppressRuleUntil_Test()
        {
            string testString = "md5.new()";
            SuppressionEx sup = new SuppressionEx(testString, "python");
            string ruleId = "DS196098";
            DateTime expirationDate = DateTime.Now.AddDays(5);

            // Suppress Rule Until test            
            string suppressedString = sup.SuppressIssue(ruleId, expirationDate);
            string expected = string.Format("md5.new() #DevSkim: ignore DS196098 until {0:yyyy}-{0:MM}-{0:dd}\n", expirationDate);
            Assert.AreEqual(expected, suppressedString, "Supress Rule Until failed ");
        }

        [TestMethod]
        public void SuppressAll_Test()
        {
            string testString = "md5.new()";
            SuppressionEx sup = new SuppressionEx(testString, "python");                      

            // Suppress All test
            string suppressedString = sup.SuppressAll();
            string expected = "md5.new() #DevSkim: ignore all\n";
            Assert.AreEqual(expected, suppressedString, "Supress All failed");
        }

        [TestMethod]
        public void SuppressAllUntil_Test()
        {
            string testString = "md5.new()";
            SuppressionEx sup = new SuppressionEx(testString, "python");            
            DateTime expirationDate = DateTime.Now.AddDays(5);

            // Suppress All Until test                    
            string suppressedString = sup.SuppressAll(expirationDate);
            string expected = string.Format("md5.new() #DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}\n", expirationDate);
            Assert.AreEqual(expected, suppressedString, "Supress All Until failed ");
        }

        [TestMethod]
        public void Multiline_IsSuppress_Test()
        {
            // Is supressed test
            string testString = "var hash=MD5.Create();";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            Assert.IsTrue(sup.Index < 0, "Suppression should not be flagged");
        }

        [TestMethod]
        public void Multiline_SuppressRule_Test()
        {
            string testString = "var hash=MD5.Create();";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            // Suppress Rule test
            string ruleId = "DS126858";
            string suppressedString = sup.SuppressIssue(ruleId);
            string expected = "var hash=MD5.Create(); /*DevSkim: ignore DS126858*/";
            Assert.AreEqual(expected, suppressedString, "Supress Rule failed ");
        }

        [TestMethod]
        public void Multiline_SuppressRuleUntil_Test()
        {
            string testString = "var hash=MD5.Create();";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            string ruleId = "DS126858";
            // Suppress Rule Until test
            DateTime expirationDate = DateTime.Now.AddDays(5);
            string suppressedString = sup.SuppressIssue(ruleId, expirationDate);
            string expected = string.Format("var hash=MD5.Create(); /*DevSkim: ignore DS126858 until {0:yyyy}-{0:MM}-{0:dd}*/", expirationDate);
            Assert.AreEqual(expected, suppressedString, "Supress Rule Until failed ");
        }

        [TestMethod]
        public void Multiline_SuppressAll_Test()
        {
            string testString = "var hash=MD5.Create();";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            // Suppress All test
            string suppressedString = sup.SuppressAll();
            string expected = "var hash=MD5.Create(); /*DevSkim: ignore all*/";
            Assert.AreEqual(expected, suppressedString, "Supress All failed");
        }

        [TestMethod]
        public void Multiline_SuppressAllUntil_Test()
        {
            string testString = "var hash=MD5.Create();";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            DateTime expirationDate = DateTime.Now.AddDays(5);
            // Suppress All Until test            
            string suppressedString = sup.SuppressAll(expirationDate);
            string expected = string.Format("var hash=MD5.Create(); /*DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}*/", expirationDate);
            Assert.AreEqual(expected, suppressedString, "Supress All Until failed ");
        }

        [TestMethod]
        public void SuppressExisting_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until {0:yyyy}-{0:MM}-{0:dd}";
            DateTime expirationDate = DateTime.Now.AddDays(5);

            SuppressionEx sup = new SuppressionEx(string.Format(testString, expirationDate), "csharp");
            Assert.IsNotNull(sup.GetSuppressedIssue("DS126858"), "Is suppressed DS126858 should be True");
            Assert.IsNotNull(sup.GetSuppressedIssue("DS168931"), "Is suppressed DS168931 should be True");

        }

        [TestMethod]
        public void SuppressMultiple_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until {0:yyyy}-{0:MM}-{0:dd}";
            DateTime expirationDate = DateTime.Now.AddDays(5);
            SuppressionEx sup = new SuppressionEx(string.Format(testString, expirationDate), "csharp");

            // Suppress multiple
            string suppressedString = sup.SuppressIssue("DS196098");
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple failed");
        }

        [TestMethod]
        public void SuppressMultipleToAll_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until {0:yyyy}-{0:MM}-{0:dd}";
            DateTime expirationDate = DateTime.Now.AddDays(5);
            SuppressionEx sup = new SuppressionEx(string.Format(testString, expirationDate), "csharp");

            // Suppress multiple to all
            string suppressedString = sup.SuppressAll();
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all failed");
        }

        [TestMethod]
        public void SuppressMultipleNewDate_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until {0:yyyy}-{0:MM}-{0:dd}";
            DateTime expirationDate = DateTime.Now.AddDays(5);
            SuppressionEx sup = new SuppressionEx(string.Format(testString, expirationDate), "csharp");

            // Suppress multiple new date            
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until {0:yyyy}-{0:MM}-{0:dd}";
            string suppressedString = sup.SuppressIssue("DS196098", DateTime.Now.AddDays(100));
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple new date failed");
        }

        [TestMethod]
        public void SuppressMultipleToAllNewDte_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until {0:yyyy}-{0:MM}-{0:dd}";
            DateTime expirationDate = DateTime.Now.AddDays(5);
            SuppressionEx sup = new SuppressionEx(string.Format(testString, expirationDate), "csharp");

            // Suppress multiple to all new date
            string suppressedString = sup.SuppressAll(DateTime.Now.AddDays(10));
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until {0:yyyy}-{0:MM}-{0:dd}";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all new date failed");
        }

        [TestMethod]
        public void UseCase_SuppressExistingPast_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until 1980-07-15";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            SuppressedIssue iss = sup.GetSuppressedIssue("DS126858");

            Assert.IsNull(sup.GetSuppressedIssue("DS126858"), "Is suppressed DS126858 should be Null");
            Assert.IsNull(sup.GetSuppressedIssue("DS168931"), "Is suppressed DS168931 should be Null");
        }

        [TestMethod]
        public void UseCase_SuppressMultiple_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until 1980-07-15";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            // Suppress multiple
            string suppressedString = sup.SuppressIssue("DS196098");
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until 1980-07-15";
            Assert.AreEqual(expected, suppressedString, "Suppress multiple failed");
        }

        [TestMethod]
        public void UseCase_SuppressMultipleNewDate_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until 1980-07-15";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            // Suppress multiple new date            
            DateTime expirationDate = DateTime.Now.AddDays(10);
            string suppressedString = sup.SuppressIssue("DS196098", expirationDate);
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931,DS196098 until 1980-07-15";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple new date failed");
        }

        [TestMethod]
        public void UseCase_SuppressMultipleToAll_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until 1980-07-15";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            DateTime expirationDate = DateTime.Now.AddDays(10);
            // Suppress multiple to all
            string suppressedString = sup.SuppressAll();
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until 1980-07-15";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all failed");
        }

        [TestMethod]
        public void UseCase_SuppressMultipleToAllNewDate_Test()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until 1980-07-15";
            SuppressionEx sup = new SuppressionEx(testString, "csharp");
            DateTime expirationDate = DateTime.Now.AddDays(10);
            // Suppress multiple to all new date
            string suppressedString = sup.SuppressAll(expirationDate);
            string expected = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore all until 1980-07-15";
            Assert.AreEqual(string.Format(expected, expirationDate), suppressedString, "Suppress multiple to all new date failed");
        }       
    }
}
