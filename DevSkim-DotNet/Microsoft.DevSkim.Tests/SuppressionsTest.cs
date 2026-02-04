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
        /// Test that XML suppression comments are generated correctly
        /// </summary>
        [TestMethod]
        public void GenerateXmlSuppression()
        {
            // Test basic XML suppression generation
            string suppression = DevSkimRuleProcessor.GenerateSuppressionByLanguage("xml", "DS123456");
            Assert.IsTrue(suppression.StartsWith("<!--"), "XML suppression should start with <!--");
            Assert.IsTrue(suppression.EndsWith("-->"), "XML suppression should end with -->");
            Assert.IsTrue(suppression.Contains("DevSkim: ignore DS123456"), "XML suppression should contain DevSkim ignore directive");
        }

        /// <summary>
        /// Test that XML suppression comments with duration are generated correctly
        /// </summary>
        [TestMethod]
        public void GenerateXmlSuppressionWithDuration()
        {
            int duration = 30;
            DateTime expectedDate = DateTime.Now.AddDays(duration);
            string expectedDateStr = expectedDate.ToString("yyyy-MM-dd");
            
            string suppression = DevSkimRuleProcessor.GenerateSuppressionByLanguage("xml", "DS123456", duration: duration);
            Assert.IsTrue(suppression.StartsWith("<!--"), "XML suppression should start with <!--");
            Assert.IsTrue(suppression.EndsWith("-->"), "XML suppression should end with -->");
            Assert.IsTrue(suppression.Contains($"until {expectedDateStr}"), "XML suppression should contain expiration date");
        }

        /// <summary>
        /// Test that XML suppression comments with reviewer are generated correctly
        /// </summary>
        [TestMethod]
        public void GenerateXmlSuppressionWithReviewer()
        {
            string suppression = DevSkimRuleProcessor.GenerateSuppressionByLanguage("xml", "DS123456", reviewerName: "TestReviewer");
            Assert.IsTrue(suppression.StartsWith("<!--"), "XML suppression should start with <!--");
            Assert.IsTrue(suppression.EndsWith("-->"), "XML suppression should end with -->");
            Assert.IsTrue(suppression.Contains("by TestReviewer"), "XML suppression should contain reviewer name");
        }

        /// <summary>
        /// Test that XML suppression comments are properly recognized by the Suppression parser
        /// </summary>
        [TestMethod]
        public void ParseXmlSuppression()
        {
            string xmlLine = "<!-- DevSkim: ignore DS123456 -->";
            Suppression suppression = new Suppression(xmlLine);
            
            Assert.IsTrue(suppression.IsInEffect, "Suppression should be in effect");
            Assert.AreEqual("DS123456", suppression.GetSuppressedIds.First(), "Suppression should contain the correct rule ID");
        }

        /// <summary>
        /// Test that XML suppression comments with expiration are properly recognized by the Suppression parser
        /// </summary>
        [TestMethod]
        public void ParseXmlSuppressionWithExpiration()
        {
            string futureDate = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");
            string xmlLine = $"<!-- DevSkim: ignore DS123456 until {futureDate} -->";
            Suppression suppression = new Suppression(xmlLine);
            
            Assert.IsTrue(suppression.IsInEffect, "Suppression should be in effect");
            Assert.AreEqual("DS123456", suppression.GetSuppressedIds.First(), "Suppression should contain the correct rule ID");
            Assert.AreEqual(DateTime.ParseExact(futureDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), 
                suppression.ExpirationDate, "Suppression should have the correct expiration date");
        }

        /// <summary>
        /// Test that expired XML suppression comments are not in effect
        /// </summary>
        [TestMethod]
        public void ParseExpiredXmlSuppression()
        {
            string pastDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            string xmlLine = $"<!-- DevSkim: ignore DS123456 until {pastDate} -->";
            Suppression suppression = new Suppression(xmlLine);
            
            Assert.IsFalse(suppression.IsInEffect, "Expired suppression should not be in effect");
            Assert.IsTrue(suppression.IsExpired, "Suppression should be marked as expired");
        }

        /// <summary>
        /// Test that XML suppression comments with reviewer are properly recognized by the Suppression parser
        /// </summary>
        [TestMethod]
        public void ParseXmlSuppressionWithReviewer()
        {
            string xmlLine = "<!-- DevSkim: ignore DS123456 by TestReviewer -->";
            Suppression suppression = new Suppression(xmlLine);
            
            Assert.IsTrue(suppression.IsInEffect, "Suppression should be in effect");
            Assert.AreEqual("DS123456", suppression.GetSuppressedIds.First(), "Suppression should contain the correct rule ID");
            Assert.AreEqual("TestReviewer", suppression.Reviewer, "Suppression should have the correct reviewer");
        }

        /// <summary>
        /// Integration test: Execute suppressions for XML files
        /// </summary>
        [TestMethod]
        public void ExecuteSuppressionsForXml()
        {
            // XML content with an MD5 reference that triggers DevSkim rule
            string xmlContent = @"<config><algorithm>MD5</algorithm></config>";

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
            Assert.IsTrue(result.Contains("<!-- DevSkim: ignore"), "XML suppression comment should be present");
            Assert.IsTrue(result.Contains("-->"), "XML suppression comment should be properly closed");

            // Verify suppression can be parsed
            string[] lines = File.ReadAllLines(sourceFile);
            var suppressionLines = lines.Where(line => line.Contains("<!-- DevSkim: ignore"));
            Assert.IsTrue(suppressionLines.Any(), "At least one suppression should be found in the file");
            foreach (string line in suppressionLines)
            {
                Suppression suppression = new Suppression(line);
                Assert.IsTrue(suppression.IsInEffect, "Suppression should be in effect");
                Assert.IsTrue(suppression.GetSuppressedIds.Length > 0, "Should have at least one suppressed rule ID");
            }
        }

        /// <summary>
        /// Integration test: Execute suppressions for XML files with reviewer and duration
        /// </summary>
        [TestMethod]
        [DataRow("TestReviewer", 30)]
        [DataRow("", 30)]
        [DataRow("TestReviewer", 0)]
        public void ExecuteSuppressionsForXmlWithReviewerAndDuration(string reviewerName, int duration)
        {
            // XML content with an MD5 reference that triggers DevSkim rule
            string xmlContent = @"<config><algorithm>MD5</algorithm></config>";

            (string basePath, string sourceFile, string sarifPath) = runAnalysis(xmlContent, "xml");

            SuppressionCommandOptions opts = new SuppressionCommandOptions
            {
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                Reviewer = reviewerName,
                Duration = duration
            };

            int resultCode = new SuppressionCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            
            string result = File.ReadAllText(sourceFile);
            Assert.IsTrue(result.Contains("<!-- DevSkim: ignore"), "XML suppression comment should be present");
            Assert.IsTrue(result.Contains("-->"), "XML suppression comment should be properly closed");

            // Verify suppression contains expected parts
            string[] lines = File.ReadAllLines(sourceFile);
            var suppressionLines = lines.Where(line => line.Contains("<!-- DevSkim: ignore"));
            foreach (string line in suppressionLines)
            {
                Suppression suppression = new Suppression(line);
                Assert.IsTrue(suppression.IsInEffect, "Suppression should be in effect");
                
                if (!string.IsNullOrEmpty(reviewerName))
                {
                    Assert.AreEqual(reviewerName, suppression.Reviewer, "Reviewer name should match");
                }
                
                if (duration > 0)
                {
                    DateTime expectedExpiration = DateTime.Now.AddDays(duration);
                    Assert.AreEqual(expectedExpiration.Date, suppression.ExpirationDate, "Expiration date should match");
                }
            }
        }

        /// <summary>
        /// Integration test: Verify that analysis respects XML suppressions in files
        /// </summary>
        [TestMethod]
        public void AnalysisRespectsXmlSuppressions()
        {
            // XML content with MD5 that already has a suppression comment
            string xmlContent = @"<config><algorithm>MD5 <!-- DevSkim: ignore DS126858 --></algorithm></config>";

            string tempFileName = $"{Path.GetTempFileName()}.xml";
            File.WriteAllText(tempFileName, xmlContent);

            DevSkimRuleSet devSkimRuleSet = DevSkimRuleSet.GetDefaultRuleSet();
            DevSkimRuleProcessor processor = new DevSkimRuleProcessor(devSkimRuleSet, new DevSkimRuleProcessorOptions()
            {
                EnableSuppressions = true
            });

            IEnumerable<Issue> issues = processor.Analyze(xmlContent, tempFileName);
            
            // The DS126858 (MD5) issue should be suppressed
            var unsuppressedIssues = issues.Where(i => !i.IsSuppressionInfo && i.Rule.Id == "DS126858");
            Assert.AreEqual(0, unsuppressedIssues.Count(), "MD5 issue should be suppressed");

            // Cleanup
            File.Delete(tempFileName);
        }

        /// <summary>
        /// Test that XML suppression is generated correctly via filename-based method
        /// </summary>
        [TestMethod]
        public void GenerateXmlSuppressionByFilename()
        {
            string suppression = DevSkimRuleProcessor.GenerateSuppressionByFileName("test.xml", "DS123456");
            Assert.IsTrue(suppression.StartsWith("<!--"), "XML suppression should start with <!--");
            Assert.IsTrue(suppression.EndsWith("-->"), "XML suppression should end with -->");
            Assert.IsTrue(suppression.Contains("DevSkim: ignore DS123456"), "XML suppression should contain DevSkim ignore directive");
        }
    }
}