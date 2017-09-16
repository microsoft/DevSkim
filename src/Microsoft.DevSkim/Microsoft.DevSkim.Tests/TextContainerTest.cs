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

        }

    }
}
