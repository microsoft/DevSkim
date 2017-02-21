using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Security.DevSkim;

namespace DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]

    public class SuppressorTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor1FailTest()
        {
            Suppressor sup = new Suppressor(null, "c");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor2FailTest()
        {
            Suppressor sup = new Suppressor("abc", null);
        }
    }
}
