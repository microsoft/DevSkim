// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Commands;
using System.Text;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.Tests
{
    [TestClass]
    public class AnalyzeTest
    {
        [TestMethod]
        public void RelativePathTest()
        {
            var tempFileName = $"{Path.GetTempFileName()}.cs";
            var outFileName = Path.GetTempFileName();
            // GetTempFileName actually makes the file
            File.Delete(outFileName);

            var basePath = Path.GetTempPath();
            var oneUpPath = Directory.GetParent(basePath).FullName;
            using var file = File.Open(tempFileName, FileMode.Create);
            file.Write(Encoding.UTF8.GetBytes("MD5;\nhttp://\n"));
            file.Close();

            var opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName,
                OutputFileFormat = "sarif",
                BasePath = basePath
            };
            new AnalyzeCommand(opts).Run();

            var resultsFile = SarifLog.Load(outFileName);
            Assert.AreEqual(1, resultsFile.Runs.Count);
            Assert.AreEqual(2, resultsFile.Runs[0].Results.Count);
            Assert.AreEqual(Path.GetFileName(tempFileName), resultsFile.Runs[0].Results[0].Locations[0].PhysicalLocation.ArtifactLocation.Uri.ToString());

            var outFileName2 = Path.GetTempFileName();

            opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName2,
                OutputFileFormat = "sarif",
                AbsolutePaths = true
            };
            new AnalyzeCommand(opts).Run();

            resultsFile = SarifLog.Load(outFileName2);
            Assert.AreEqual(1, resultsFile.Runs.Count);
            Assert.AreEqual(2, resultsFile.Runs[0].Results.Count);
            Assert.AreEqual(new Uri(tempFileName).GetFilePath(), resultsFile.Runs[0].Results[0].Locations[0].PhysicalLocation.ArtifactLocation.Uri.GetFilePath());

            var outFileName3 = Path.GetTempFileName();
            
            opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName3,
                OutputFileFormat = "sarif"
            };
            new AnalyzeCommand(opts).Run();
            
            resultsFile = SarifLog.Load(outFileName3);
            Assert.AreEqual(1, resultsFile.Runs.Count);
            Assert.AreEqual(2, resultsFile.Runs[0].Results.Count);
            // If no base path is specified, the base path is rooted in by the Path argument
            Assert.AreEqual(Path.GetFileName(tempFileName), resultsFile.Runs[0].Results[0].Locations[0].PhysicalLocation.ArtifactLocation.Uri.GetFilePath());

            var outFileName4 = Path.GetTempFileName();

            opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName4,
                OutputFileFormat = "sarif",
                BasePath = Directory.GetCurrentDirectory()
            };
            new AnalyzeCommand(opts).Run();

            resultsFile = SarifLog.Load(outFileName4);
            Assert.AreEqual(1, resultsFile.Runs.Count);
            Assert.AreEqual(2, resultsFile.Runs[0].Results.Count);
            
            // The path to CWD isnt relative 
            Assert.AreEqual(resultsFile.Runs[0].Results[0].Locations[0].PhysicalLocation.ArtifactLocation.Uri.GetFilePath(),Path.GetRelativePath(Directory.GetCurrentDirectory(), tempFileName));
        }
        
        [DataTestMethod]
        [DataRow("DS126858","DS126858")]
        [DataRow("DS137138","DS137138")]
        public void TestFilterByIds(string idToLimitTo, string idToExpect)
        {
            var tempFileName = $"{Path.GetTempFileName()}.cs";
            var outFileName = Path.GetTempFileName();
            // GetTempFileName actually makes the file
            File.Delete(outFileName);
            using var file = File.Open(tempFileName, FileMode.Create);
            file.Write(Encoding.UTF8.GetBytes("MD5;\nhttp://\n"));
            file.Close();

            var opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName,
                OutputFileFormat = "sarif",
                RuleIds = new []{ idToLimitTo }
            };
            new AnalyzeCommand(opts).Run();
            
            var resultCode = new AnalyzeCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            var resultsFile = SarifLog.Load(outFileName);
            Assert.AreEqual(1, resultsFile.Runs.Count);
            Assert.AreEqual(1, resultsFile.Runs[0].Results.Count);
            Assert.AreEqual(idToExpect, resultsFile.Runs[0].Results[0].RuleId);
        }
        
        [DataTestMethod]
        [DataRow("DS126858","DS137138")]
        [DataRow("DS137138","DS126858")]
        public void TestIgnoreIds(string idToIgnore, string idToExpect)
        {
            var tempFileName = $"{Path.GetTempFileName()}.cs";
            var outFileName = Path.GetTempFileName();
            // GetTempFileName actually makes the file
            File.Delete(outFileName);
            using var file = File.Open(tempFileName, FileMode.Create);
            file.Write(Encoding.UTF8.GetBytes("MD5;\nhttp://\n"));
            file.Close();

            var opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName,
                OutputFileFormat = "sarif",
                IgnoreRuleIds = new []{ idToIgnore }
            };
            new AnalyzeCommand(opts).Run();
            
            var resultCode = new AnalyzeCommand(opts).Run();
            Assert.AreEqual(0, resultCode);
            var resultsFile = SarifLog.Load(outFileName);
            Assert.AreEqual(1, resultsFile.Runs.Count);
            Assert.AreEqual(1, resultsFile.Runs[0].Results.Count);
            Assert.AreEqual(idToExpect, resultsFile.Runs[0].Results[0].RuleId);
        }
    }
}