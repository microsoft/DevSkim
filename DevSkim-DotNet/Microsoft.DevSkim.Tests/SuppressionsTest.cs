// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Commands;
using System.Text;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class SupprestionTest
    {
        [TestMethod]
        public void ExecuteSuppressions()
        {
            (string basePath, string sourceFile, string sarifPath)  = runAnalysis(@"MD5;
            http://", "c");

            var opts = new SuppressionCommandOptions{
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true
            };

            var resultCode = new SuppressionCommand(opts).Run();
            var result = File.ReadAllText(sourceFile);

            Assert.IsTrue(result.Contains("MD5;/* DevSkim: ignore DS126858 */"));
            Assert.IsTrue(result.Contains("http:///* DevSkim: ignore DS137138 */"));
        }


        [TestMethod]
        public void NotExecuteSuppressions()
        {
            (string basePath, string sourceFile, string sarifPath)  = runAnalysis(@"MD5;
            http://", "c");

            var opts = new SuppressionCommandOptions{
                Path = basePath,
                SarifInput = sarifPath,
            };

            var resultCode = new SuppressionCommand(opts).Run();
            var result = File.ReadAllText(sourceFile);

            Assert.IsFalse(result.Contains("MD5;/* DevSkim: ignore DS126858 */"));
            Assert.IsFalse(result.Contains("http:///* DevSkim: ignore DS137138 */"));
        }

        [TestMethod]
        public void ExecuteSuppressionsOnSpecificFiles()
        {
            (string basePath, string sourceFile, string sarifPath)  = runAnalysis(@"MD5;
            http://", "c");

            var opts = new SuppressionCommandOptions{
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true,
                FilesToApplyTo = new string[]{"/tmp/not-existing.c"}
            };

            var resultCode = new SuppressionCommand(opts).Run();
            var result = File.ReadAllText(sourceFile);

            Assert.IsFalse(result.Contains("MD5;/* DevSkim: ignore DS126858 */"));
            Assert.IsFalse(result.Contains("http:///* DevSkim: ignore DS137138 */"));
        }

        [TestMethod]
        public void ExecuteSuppresionsForMultilineFormattedFiles()
        {
            (string basePath, string sourceFile, string sarifPath)  = runAnalysis(@"MD5 \
            Test;
            http://", "c");

            var opts = new SuppressionCommandOptions{
                Path = basePath,
                SarifInput = sarifPath,
                ApplyAllSuppression = true
            };

            var resultCode = new SuppressionCommand(opts).Run();
            var result = File.ReadAllText(sourceFile);

            Assert.IsTrue(result.Contains(@"/* DevSkim: ignore DS126858 */MD5 \"));
            Assert.IsTrue(result.Contains("http:///* DevSkim: ignore DS137138 */"));
        }


        private (string basePath, string sourceFile, string sarifPath) runAnalysis(string content, string ext) {
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