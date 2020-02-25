// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class SuppressorTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor1FailTest()
        {
            Suppression sup = new Suppression(null);
        }

        [TestMethod]
        public void IsNotSuppressedTest()
        {
            // Is supressed test
            string testString = "md5.new()";
            Suppression sup = new Suppression(testString);
            Assert.IsTrue(sup.Index < 0, "Suppression should not be flagged");
        }

        [TestMethod]
        public void IsSuppressedTest()
        {
            // Is supressed test
            string testString = "md5.new() #DevSkim: ignore DS196098";
            Suppression sup = new Suppression(testString);
            Assert.IsTrue(sup.GetIssues().Length == 1, "Suppression should be flagged");
        }

        public void SuppressedIndexTest()
        {
            // Is supressed test
            string testString = "md5.new() #DevSkim: ignore DS196098";
            Suppression sup = new Suppression(testString);
            Assert.IsTrue(sup.Index == 12, "Suppression should start in ondex 12");
        }

        [TestMethod]
        public void SuppresseedAll_Test()
        {
            string testString = "var hash=MD5.Create(); /*DevSkim: ignore all*/";
            Suppression sup = new Suppression(testString);
            // Suppress All test
            Assert.IsTrue(sup.GetIssues().Length == 1, "Supress All failed");
        }

        [TestMethod]
        public void GetSuppressedTest()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931";
            Suppression sup = new Suppression(testString);
            SuppressedIssue iss = sup.GetSuppressedIssue("DS126858");

            Assert.IsNotNull(sup.GetSuppressedIssue("DS126858"), "Is suppressed DS126858 should be instance");
            Assert.IsNotNull(sup.GetSuppressedIssue("DS168931"), "Is suppressed DS168931 should be instance");
        }

        [TestMethod]
        public void GetNotSuppressedTest()
        {
            string testString = "MD5 hash = new MD5CryptoServiceProvider(); //DevSkim: ignore DS126858,DS168931 until 1980-07-15";
            Suppression sup = new Suppression(testString);
            SuppressedIssue iss = sup.GetSuppressedIssue("DS126858");

            Assert.IsNull(sup.GetSuppressedIssue("DS126858"), "Is suppressed DS126858 should be Null");
            Assert.IsNull(sup.GetSuppressedIssue("DS168931"), "Is suppressed DS168931 should be Null");
        }
    }
}
