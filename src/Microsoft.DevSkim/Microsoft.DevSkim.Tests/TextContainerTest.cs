using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TextContainerTest
    {
        [TestMethod]
        public void Container_Test()
        {
            string text = "First Line\nSecond Dwarf\nThird Line\nFourth Line\n";
            TextContainer cont = new TextContainer(text);
            int index = text.IndexOf("Dwarf");
            Location loc = cont.GetLocation(index);

            Assert.AreEqual(2, loc.Line);
            Assert.AreEqual(8, loc.Column);

            loc = cont.GetLocation(text.Length-1);
            Assert.AreEqual(4, loc.Line);
            Assert.AreEqual(12, loc.Column);

            Ruleset rules = Ruleset.FromDirectory(@"rules\valid", null);            

            RuleProcessor processor = new RuleProcessor(rules)
            {
                EnableSuppressions = true
            };

            // MD5CryptoServiceProvider test
            string testString = "<h:table xmlns:h=\"http://www.w3.org/TR/html4/\">";
            Issue[] issues = processor.Analyze(testString, "xml");
            Assert.AreEqual(0, issues.Length);

            testString = "echo(urlencode($_POST['data']);";
            issues = processor.Analyze(testString, "php");
            Assert.AreEqual(0, issues.Length);
        }

    }
}
