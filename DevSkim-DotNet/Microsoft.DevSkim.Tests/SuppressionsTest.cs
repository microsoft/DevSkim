// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Commands;
using System.Text;
using Microsoft.DevSkim.CLI;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class SuppressionTest
    {
        public TestContext? TestContext { get; set; }

        [DataTestMethod]
        [DataRow("", 30)]
        [DataRow("Contoso", 30)]
        public void ExecuteSuppressionsWithReviewerNameAndDate(string reviewerName, int duration)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://contoso.com", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                Reviewer = reviewerName,
                Duration = duration
            };
            DateTime expectedExpiration = DateTime.Now.AddDays(duration);
            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string[] result = File.ReadAllLines(sourceFile);
            Suppression firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds.First());
            Assert.AreEqual(reviewerName, firstLineSuppression.Reviewer);
            Assert.AreEqual(expectedExpiration.Date, firstLineSuppression.ExpirationDate);
            Suppression secondLineSuppression = new Suppression(result[1]);
            Assert.IsTrue(secondLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", secondLineSuppression.GetSuppressedIds.First());
            Assert.AreEqual(reviewerName, secondLineSuppression.Reviewer);
            Assert.AreEqual(expectedExpiration.Date, secondLineSuppression.ExpirationDate);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("Contoso")]
        public void ExecuteSuppressionsWithReviewerName(string reviewerName)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://contoso.com", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                Reviewer = reviewerName
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string[] result = File.ReadAllLines(sourceFile);
            Suppression firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds.First());
            Assert.AreEqual(reviewerName, firstLineSuppression.Reviewer);
            Suppression secondLineSuppression = new Suppression(result[1]);
            Assert.IsTrue(secondLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", secondLineSuppression.GetSuppressedIds.First());
            Assert.AreEqual(reviewerName, secondLineSuppression.Reviewer);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppressions(bool preferMultiLine)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://contoso.com", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLine
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string[] result = File.ReadAllLines(sourceFile);
            Suppression firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds.First());
            Suppression secondLineSuppression = new Suppression(result[1]);
            Assert.IsTrue(secondLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", secondLineSuppression.GetSuppressedIds.First());
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteMultipleSuppressionsInOneLine(bool preferMultiLineFormat)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;http://contoso.com", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLineFormat
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string[] result = File.ReadAllLines(sourceFile);
            Suppression firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", firstLineSuppression.GetSuppressedIds[0]);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds[1]);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppressionsOnlyForSpecifiedRules(bool preferMultiLineFormat)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;http://contoso.com", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLineFormat,
                RulesToApplyFrom = new string[] { "DS137138" }
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string[] result = File.ReadAllLines(sourceFile);
            Suppression firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", firstLineSuppression.GetSuppressedIds[0]);
            Assert.AreEqual(1, firstLineSuppression.GetSuppressedIds.Length);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppressionsWithExpiration(bool preferMultiLineFormat)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;http://contoso.com", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLineFormat,
                Duration = 30
            };
            DateTime expectedExpiration = DateTime.Now.AddDays(30);
            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string[] result = File.ReadAllLines(sourceFile);
            Suppression firstLineSuppression = new Suppression(result[0]);
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

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                PreferMultiline = preferMultiLine
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual((int)ExitCode.CriticalError, resultCode);
            string result = File.ReadAllText(sourceFile);
            Assert.IsFalse(result.Contains("DevSkim: ignore"));
        }

        [TestMethod]
        public void ExecuteDryRun()
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                DryRun = true,
            };

            int resultCode = new SuppressionCommand(opts).Run();
            string result = File.ReadAllText(sourceFile);

            Assert.IsFalse(result.Contains("DevSkim: ignore"));
        }

        [TestMethod]
        public void ExecuteSuppressionsOnSpecificFiles()
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5;
            http://", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                FilesToApplyTo = new string[] { "/tmp/not-existing.c" }
            };

            int resultCode = new SuppressionCommand(opts).Run();
            string result = File.ReadAllText(sourceFile);

            Assert.IsFalse(result.Contains("DevSkim: ignore"));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExecuteSuppresionsForMultilineFormattedFiles(bool preferMultiLine)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(@"MD5 \
            Test;
            http://contoso.com", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLine
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string[] result = File.ReadAllLines(sourceFile);
            Suppression firstLineSuppression = new Suppression(result[0]);
            Assert.IsTrue(firstLineSuppression.IsInEffect);
            Assert.AreEqual("DS126858", firstLineSuppression.GetSuppressedIds.First());
            Suppression secondLineSuppression = new Suppression(result[2]);
            Assert.IsTrue(secondLineSuppression.IsInEffect);
            Assert.AreEqual("DS137138", secondLineSuppression.GetSuppressedIds.First());
        }

        private (string basePath, string sourceFile, string sarifPath) runAnalysis(string content, string ext)
        {
            string tempFileName = $"{Path.GetTempFileName()}.{ext}";
            string outFileName = Path.GetTempFileName();
            File.Delete(outFileName);

            string basePath = Path.GetTempPath();
            using FileStream file = File.Open(tempFileName, FileMode.Create);
            file.Write(Encoding.UTF8.GetBytes(content));
            file.Close();

            AnalyzeCommandOptions opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName,
                OutputFileFormat = "sarif",
                BasePath = basePath
            };

            new AnalyzeCommand(opts).Run();
            return (basePath, tempFileName, Path.Combine(basePath, outFileName));
        }
        
        /// <summary>
        /// Test that suppressing an issue doesn't change the line break characters
        /// </summary>
        /// <param name="lineBreakSequence">Character sequence used for line breaks</param>
        /// <param name="preferMultiLine">Use multiline comments or not</param>
        [DataTestMethod]
        [DataRow("\r\n", true)]
        [DataRow("\r\n", false)]
        [DataRow("\n", true)]
        [DataRow("\n", false)]
        public void DontPerturbExtantLineBreaks(string lineBreakSequence, bool preferMultiLine)
        {
            (string basePath, string sourceFile, string sarifPath) = runAnalysis($"MD5\\{lineBreakSequence}http://contoso.com{lineBreakSequence}", "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                PreferMultiline = preferMultiLine
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string result = File.ReadAllText(sourceFile);
            Assert.AreEqual(lineBreakSequence, result[^lineBreakSequence.Length..]);
        }
        
        /// <summary>
        /// Test that files don't change at all when they have findings but those rule ids are not selected for suppression
        /// </summary>
        /// <param name="lineBreakSequence">Character sequence used for line breaks</param>
        /// <param name="preferMultiLine">Use multiline comments or not</param>
        [DataTestMethod]
        [DataRow("\r\n", true)]
        [DataRow("\r\n", false)]
        [DataRow("\n", true)]
        [DataRow("\n", false)]
        public void DontChangeFilesWithoutSelectedFindings(string lineBreakSequence, bool preferMultiline)
        {
            string originalContent = $"MD5{lineBreakSequence}http://contoso.com{lineBreakSequence}";
            (string basePath, string sourceFile, string sarifPath) = runAnalysis(originalContent, "c");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = false,
                PreferMultiline = preferMultiline,
                RulesToApplyFrom = new string[] { "NotAValidRuleId" } // Don't apply any rules
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            string result = File.ReadAllText(sourceFile);
            Assert.AreEqual(originalContent, result);
        }

        /// <summary>
        /// Integration test for XML suppressions
        /// </summary>
        [TestMethod]
        public void ExecuteSuppressionsForXML()
        {
            // Properly formatted XML content with patterns that trigger DevSkim rules (MD5 and http://)
            string xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<configuration>
    <security>
        <algorithm>MD5</algorithm>
    </security>
    <endpoint>http://example.com</endpoint>
</configuration>";

            (string basePath, string sourceFile, string sarifPath) = runAnalysis(xmlContent, "xml");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            
            string result = File.ReadAllText(sourceFile);
            
            // Verify that XML-style suppressions were added
            Assert.Contains("<!-- DevSkim: ignore", result, "XML suppression comment should be present");
            Assert.Contains("-->", result, "XML suppression comment should be properly closed");

            // Verify suppressions are in correct format
            string[] lines = File.ReadAllLines(sourceFile);
            bool foundSuppression = false;
            foreach (string line in lines)
            {
                if (line.Contains("<!-- DevSkim: ignore"))
                {
                    foundSuppression = true;
                    Suppression suppression = new Suppression(line);
                    Assert.IsTrue(suppression.IsInEffect, "Suppression should be in effect");
                    Assert.IsGreaterThan(0,suppression.GetSuppressedIds.Length, "Should have at least one suppressed rule ID");
                }
            }
            Assert.IsTrue(foundSuppression, "At least one suppression should be found in the file");
        }

        /// <summary>
        /// Integration test for XML suppressions with reviewer and duration
        /// </summary>
        [TestMethod]
        [DataRow("", 30, DisplayName = "XML Suppression with Duration only")]
        [DataRow("TestReviewer", 30, DisplayName = "XML Suppression with Reviewer and Duration")]
        [DataRow("TestReviewer", 0, DisplayName = "XML Suppression with Reviewer only")]
        public void ExecuteSuppressionsForXMLWithReviewerAndDuration(string reviewerName, int duration)
        {
            // Properly formatted XML content with patterns that trigger DevSkim rules (MD5 and http://)
            string xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<configuration>
    <security>
        <algorithm>MD5</algorithm>
    </security>
    <endpoint>http://example.com</endpoint>
</configuration>";

            (string basePath, string sourceFile, string sarifPath) = runAnalysis(xmlContent, "xml");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                Reviewer = reviewerName,
                Duration = duration
            };

            DateTime expectedExpiration = duration > 0 ? DateTime.Now.AddDays(duration) : DateTime.Now;
            
            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            
            string result = File.ReadAllText(sourceFile);
            Assert.Contains("<!-- DevSkim: ignore", result, "XML suppression comment should be present");
            
            // Verify suppressions contain reviewer and/or duration
            string[] lines = File.ReadAllLines(sourceFile);
            bool foundSuppression = false;
            foreach (string line in lines)
            {
                if (line.Contains("<!-- DevSkim: ignore"))
                {
                    foundSuppression = true;
                    Suppression suppression = new Suppression(line);
                    Assert.IsTrue(suppression.IsInEffect, "Suppression should be in effect");
                    
                    if (!string.IsNullOrEmpty(reviewerName))
                    {
                        Assert.AreEqual(reviewerName, suppression.Reviewer, "Reviewer name should match");
                    }
                    
                    if (duration > 0)
                    {
                        //Assert.IsNotNull(suppression.ExpirationDate, "Expiration date should be set");
                        Assert.AreEqual(expectedExpiration.Date, suppression.ExpirationDate, "Expiration date should match");
                    }
                }
            }
            Assert.IsTrue(foundSuppression, "At least one suppression should be found in the file");
        }
    }
}