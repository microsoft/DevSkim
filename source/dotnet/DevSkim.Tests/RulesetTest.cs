using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Security.DevSkim;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class RulesetTest
    {
        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void InvalidRuleFileFailTest()
        {
            Ruleset ruleset = Ruleset.FromFile("x:\\file.txt", null);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void InvalidRuleDirectoryFailTest()
        {
            Ruleset ruleset = Ruleset.FromDirectory("x:\\invalid_directory", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InvalidRuleStringFailTest()
        {
            Ruleset ruleset = Ruleset.FromDirectory(null, null);
        }
    }
}
