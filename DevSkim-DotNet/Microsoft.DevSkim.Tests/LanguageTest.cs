// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class LanguageTest
    {
        [TestMethod]
        public void LanguageCommentPrefixTest()
        {
            string lang = "csharp";
            string prefix = Language.GetCommentInline(lang);
            Assert.AreEqual("//", prefix, "Incorect prefix for " + lang);

            lang = "python";
            prefix = Language.GetCommentInline(lang);
            Assert.AreEqual("#", prefix, "Incorect prefix for " + lang);

            lang = "sql";
            prefix = Language.GetCommentInline(lang);
            Assert.AreEqual("--", prefix, "Incorect prefix for " + lang);

            lang = "klyngon";
            prefix = Language.GetCommentInline(lang);
            Assert.AreEqual(string.Empty, prefix, "Incorect prefix for " + lang);

            lang = null;
            prefix = Language.GetCommentInline(lang);
            Assert.AreEqual(string.Empty, prefix, "Incorect prefix for " + lang);
        }

        [TestMethod]
        public void LanguageCommentSuffixTest()
        {
            string lang = "csharp";
            string suffix = Language.GetCommentSuffix(lang);
            Assert.AreEqual("*/", suffix, "Incorect suffix for " + lang);

            lang = "python";
            suffix = Language.GetCommentSuffix(lang);
            Assert.AreEqual("\n", suffix, "Incorect suffix for " + lang);

            lang = "klyngon";
            suffix = Language.GetCommentSuffix(lang);
            Assert.AreEqual(string.Empty, suffix, "Incorect suffix for " + lang);

            lang = null;
            suffix = Language.GetCommentSuffix(lang);
            Assert.AreEqual(string.Empty, suffix, "Incorect suffix for " + lang);
        }

        [TestMethod]
        public void LanguageQueryTest()
        {
            string file = @"c:\myproject\program.cs";
            string lang = Language.FromFileName(file);
            Assert.AreEqual("csharp", lang, "Incorect language was return for " + file);

            file = @"program.cs";
            lang = Language.FromFileName(file);
            Assert.AreEqual("csharp", lang, "Incorect language was return for " + file);

            file = @"program.js";
            lang = Language.FromFileName(file);
            Assert.AreEqual("javascript", lang, "Incorect language was return for " + file);

            file = @"query.sql";
            lang = Language.FromFileName(file);
            Assert.AreEqual("sql", lang, "Incorect language was return for " + file);

            file = @"packages.config";
            lang = Language.FromFileName(file);
            Assert.AreEqual("packages.config", lang, "Incorect language was return for " + file);

            file = @"program.klyngon";
            lang = Language.FromFileName(file);
            Assert.AreEqual(string.Empty, lang, "Incorect language was return for " + file);

            file = null;
            lang = Language.FromFileName(file);
            Assert.AreEqual(string.Empty, lang, "Incorect language was return for " + file);
        }
    }
}