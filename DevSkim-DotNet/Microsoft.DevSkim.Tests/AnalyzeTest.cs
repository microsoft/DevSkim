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

        public const string contentToTest = @"MD5;
// MD5
/* MD5
*/";
        
        public const string languageFileContent = @"[
    {
        ""name"": ""lorem"",
        ""extensions"": [ "".xx"" ]
    }
]";

        public const string commentFileContent = @"[
    {
        ""language"": 
        [
            ""lorem""
        ],
        ""inline"": ""//"",
        ""prefix"": ""/*"",
        ""suffix"": ""*/""
    }
  ]";
        
        public const string ruleFileContent = @"[
    {
        ""name"": ""Win32 - Hard-coded SSL/TLS Protocol"",
        ""id"": ""DSTEST"",
        ""description"": ""Test rule that should match cs but not other languages"",
        ""applies_to"": [
            ""lorem""
        ],
        ""tags"": [
            ""Tests.LanguagesAndComments""
        ],
        ""severity"": ""ManualReview"",
        ""confidence"" : ""High"",
        ""rule_info"": ""DSTEST.md"",
        ""patterns"": [
            {
                ""pattern"": ""MD5"",
                ""type"": ""regex"",
                ""scopes"": [
                    ""code""
                ]
            }
        ]
    }
]";
        
        [DataRow(commentFileContent, languageFileContent, ruleFileContent, 0, 1)]
        [DataRow(languageFileContent, commentFileContent, ruleFileContent, 2, 1)]
        [DataRow(commentFileContent, commentFileContent, ruleFileContent, 2, 1)]
        [DataRow(languageFileContent, languageFileContent, ruleFileContent, 0, 3)] // We would expect this to fail, but failing to deserialize comments fails open currently in AI, thus comments are ignored, thus 3 results

        [DataTestMethod]
        public void TestCustomLanguageAndComments(string commentFileContentIn, string languageFileContentIn, string ruleFileContentIn, int expectedExitCode, int expectedNumResults)
        {
            var tempFileName = PathHelper.GetRandomTempFile("xx");
            var languagesFileName = PathHelper.GetRandomTempFile("json");
            var commentsFileName = PathHelper.GetRandomTempFile("json");
            var ruleFileName = PathHelper.GetRandomTempFile("json");
            var outFileName = PathHelper.GetRandomTempFile("sarif");
            using var file = File.Open(tempFileName, FileMode.Create);
            file.Write(Encoding.UTF8.GetBytes(contentToTest));
            file.Close();
            using var languageFile = File.Open(languagesFileName, FileMode.Create);
            languageFile.Write(Encoding.UTF8.GetBytes(languageFileContentIn));
            languageFile.Close();
            using var commentsFile = File.Open(commentsFileName, FileMode.Create);
            commentsFile.Write(Encoding.UTF8.GetBytes(commentFileContentIn));
            commentsFile.Close();
            using var ruleFile = File.Open(ruleFileName, FileMode.Create);
            ruleFile.Write(Encoding.UTF8.GetBytes(ruleFileContentIn));
            ruleFile.Close();
            var opts = new AnalyzeCommandOptions()
            {
                Path = tempFileName,
                OutputFile = outFileName,
                OutputFileFormat = "sarif",
                CommentsPath = commentsFileName,
                LanguagesPath = languagesFileName,
                Rules = new []{ ruleFileName },
                IgnoreDefaultRules = true
            };
            new AnalyzeCommand(opts).Run();
            
            var resultCode = new AnalyzeCommand(opts).Run();
            Assert.AreEqual(expectedExitCode, resultCode);
            if (expectedExitCode == 0)
            {
                var resultsFile = SarifLog.Load(outFileName);
                Assert.AreEqual(1, resultsFile.Runs.Count);
                Assert.AreEqual(expectedNumResults, resultsFile.Runs[0].Results.Count);
                if (expectedNumResults > 0)
                {
                    Assert.AreEqual("DSTEST", resultsFile.Runs[0].Results[0].RuleId);
                }
            }
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