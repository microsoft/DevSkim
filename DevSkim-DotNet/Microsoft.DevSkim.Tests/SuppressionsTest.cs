// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Commands;
using System.Text;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class SuppressionTest
    {
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppressions(bool preferMultiLine)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLine
            };

            var resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            var result = File.ReadAllLines(sourceFile);
            var firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds.First());
            var secondLineSuppression = new Suppression(result[1]);
            Assert.IsTrue(secondLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", secondLineSuppression.GetSuppressedIds.First());
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteMultipleSuppressionsInOneLine(bool preferMultiLineFormat)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLineFormat
            };

            var resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            var result = File.ReadAllLines(sourceFile);
            var firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", firstLineSuppression.GetSuppressedIds[0]);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds[1]);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppressionsOnlyForSpecifiedRules(bool preferMultiLineFormat)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLineFormat,
                RulesToApplyFrom = new string[] { "DS137138" }
            };

            var resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            var result = File.ReadAllLines(sourceFile);
            var firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", firstLineSuppression.GetSuppressedIds[0]);
            Assert.AreEqual(1, firstLineSuppression.GetSuppressedIds.Length);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppressionsWithExpiration(bool preferMultiLineFormat)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLineFormat,
                Duration = 30
            };
            var expectedExpiration = DateTime.Now.AddDays(30);
            var resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            var result = File.ReadAllLines(sourceFile);
            var firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", firstLineSuppression.GetSuppressedIds[0]);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds[1]);
            Assert.AreEqual(expectedExpiration.Date, firstLineSuppression.ExpirationDate);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void NotExecuteSuppressions(bool preferMultiLine)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                PreferMultiline = preferMultiLine
            };

            var resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(2, resultCode);
            var result = File.ReadAllText(sourceFile);
            Assert.IsFalse(result.Contains("DevSkim: ignore"));
        }

        [TestMethod]
        public void ExecuteDryRun()
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                DryRun = true,
            };

            var resultCode = new SuppressionCommand(opts).Run();
            var result = File.ReadAllText(sourceFile);

            Assert.IsFalse(result.Contains("DevSkim: ignore"));
        }

        [TestMethod]
        public void ExecuteSuppressionsOnSpecificFiles()
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                FilesToApplyTo = new string[] { "/tmp/not-existing.c" }
            };

            var resultCode = new SuppressionCommand(opts).Run();
            var result = File.ReadAllText(sourceFile);

            Assert.IsFalse(result.Contains("DevSkim: ignore"));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppresionsForMultilineFormattedFiles(bool preferMultiLine)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5 \
            Test;
            http://", "c");

            var opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLine
            };

            var resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            var result = File.ReadAllLines(sourceFile);
            var firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds.First());
            var secondLineSuppression = new Suppression(result[2]);
            Assert.IsTrue(secondLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", secondLineSuppression.GetSuppressedIds.First());
        }

        private (string basePath, string sourceFile, string sarifPath) runAnalysis(string content, string ext)
        {
            var tempFileName = $"{Path.GetTempFileName()}.{ext}";
            var outFileName = Path.GetTempFileName();
            File.Delete(outFileName);

            var basePath = Path.GetTempPath();
            var oneUpPath = Directory.GetParent(basePath).FullName;
            using var file = File.Open(tempFileName, FileMode.Create);
            file.Write(Encoding.UTF8.GetBytes(content));
            file.Close();

            var opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName,
                OutputFileFormat = "sarif",
                BasePath = basePath
            };

            new AnalyzeCommand(opts).Run();
            return (basePath, tempFileName, Path.Combine(basePath, outFileName));
        }
    }
}