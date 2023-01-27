// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.DevSkim.CLI.Writers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using LibGit2Sharp;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim.CLI.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class AnalyzeCommand
    {
        private AnalyzeCommandOptions opts;

        public AnalyzeCommand(AnalyzeCommandOptions options)
        {
            opts = options;
        }

        public int Run()
        {
            if (opts.SuppressError)
            {
                Console.SetError(StreamWriter.Null);
            }

            if (!Directory.Exists(opts.Path) && !File.Exists(opts.Path))
            {
                Debug.WriteLine("Error: Not a valid file or directory {0}", opts.Path);

                return (int)ExitCode.CriticalError;
            }
            
            var fp = Path.GetFullPath(opts.Path);

            if (string.IsNullOrEmpty(opts.BasePath))
            {
                opts.BasePath = fp;
            }

            IEnumerable<FileEntry> fileListing;
            var extractor = new Extractor();
            var extractorOpts = new ExtractorOptions() { ExtractSelfOnFail = false, DenyFilters = opts.Globs };
            // Analysing a single file
            if (!Directory.Exists(fp))
            {
                if (opts.RespectGitIgnore)
                {
                    if (IsGitPresent())
                    {
                        if (IsGitIgnored(fp))
                        {
                            Console.WriteLine("The file specified was ignored by gitignore.");
                            return (int)ExitCode.CriticalError;
                        }

                        fileListing = FilePathToFileEntries(opts, fp, extractor, extractorOpts);
                    }
                    else
                    {
                        Console.WriteLine("Could not detect git on path. Unable to use gitignore.");
                        return (int)ExitCode.CriticalError;
                    }
                }
                else
                {
                    fileListing = FilePathToFileEntries(opts, fp, extractor, extractorOpts);
                }
            }
            // Analyzing a directory
            else
            {
                if (opts.RespectGitIgnore)
                {
                    if (IsGitPresent())
                    {
                        var innerList = new List<FileEntry>();
                        var files = Directory.EnumerateFiles(fp, "*.*", SearchOption.AllDirectories)
                            .Where(fileName => !IsGitIgnored(fileName));
                        foreach (var notIgnoredFileName in files)
                        {
                            innerList.AddRange(
                                FilePathToFileEntries(opts, notIgnoredFileName, extractor, extractorOpts));
                        }

                        fileListing = innerList;
                    }
                    else
                    {                        
                        Console.WriteLine("Could not detect git on path. Unable to use gitignore.");
                        return (int)ExitCode.CriticalError;
                    }
                }
                else
                {
                    var innerList = new List<FileEntry>();
                    var files = Directory.EnumerateFiles(fp, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        innerList.AddRange(FilePathToFileEntries(opts, file, extractor, extractorOpts));
                    }

                    fileListing = innerList;                
                }
            }

            Languages? languages = null;
            if (!string.IsNullOrEmpty(opts.CommentsPath) || !string.IsNullOrEmpty(opts.LanguagesPath))
            {
                if (string.IsNullOrEmpty(opts.CommentsPath) || string.IsNullOrEmpty(opts.LanguagesPath))
                {
                    Console.Error.WriteLine("When either comments or languages are specified both must be specified.");
                    return (int)ExitCode.ArgumentParsingError;
                }

                try
                {
                    languages = DevSkimLanguages.FromFiles(opts.CommentsPath, opts.LanguagesPath);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Either the Comments or Languages file was not able to be read. ({e.Message})");
                    return (int)ExitCode.ArgumentParsingError;
                }
            }
            languages ??= DevSkimLanguages.LoadEmbedded();
            return RunFileEntries(fileListing, languages);
        }

        /// <summary>
        /// Based on the options, return an enumeration of the files from the path.  For example, if crawl archives is set, will crawl into archives if possible, otherwise just returns the file itself in a FileEntry wrapper
        /// </summary>
        /// <param name="opts"></param>
        /// <param name="file"></param>
        /// <param name="extractor"></param>
        /// <param name="extractorOptions"></param>
        /// <returns></returns>
        private static IEnumerable<FileEntry> FilePathToFileEntries(AnalyzeCommandOptions opts, string file, Extractor extractor, ExtractorOptions extractorOptions)
        {
            if (opts.CrawlArchives)
            {
                return extractor.Extract(file,extractorOptions);
            }

            return extractorOptions.FileNamePasses(file) ? FilenameToFileEntryArray(file) : Array.Empty<FileEntry>();
        }

        /// <summary>
        /// Checks if the file path is ignored by git
        /// </summary>
        /// <param name="fp"></param>
        /// <returns></returns>
        private static bool IsGitIgnored(string fp)
        {
            var process = Process.Start(new ProcessStartInfo("git")
            {
                Arguments = $"check-ignore {fp}",
                WorkingDirectory = Directory.GetParent(fp)?.FullName,
                RedirectStandardOutput = true
            });
            process?.WaitForExit();
            var stdOut = process?.StandardOutput.ReadToEnd();
            return process?.ExitCode == 0 && stdOut?.Length > 0;
        }

        /// <summary>
        /// Checks if git is available on the path
        /// </summary>
        /// <returns></returns>
        private static bool IsGitPresent()
        {
            var process = Process.Start(new ProcessStartInfo("git")
            {
                Arguments = "--version"
            });
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }

        string TryRelativizePath(string parentPath, string childPath)
        {
            try
            {
                if (opts.AbsolutePaths)
                {
                    return Path.GetFullPath(childPath);
                }
                if (parentPath == childPath)
                {
                    if (File.Exists(parentPath))
                    {
                        return Path.GetFileName(childPath);
                    }
                }
                return Path.GetRelativePath(parentPath, childPath) ?? childPath;
            }
            catch (Exception)
            {
                // Paths weren't relative.
            }
            return childPath;
        }

        private int RunFileEntries(IEnumerable<FileEntry> fileListing, Languages devSkimLanguages)
        {
            DevSkimRuleSet devSkimRuleSet = opts.IgnoreDefaultRules ? new() : DevSkimRuleSet.GetDefaultRuleSet();
            if (opts.Rules.Any())
            {
                foreach (var path in opts.Rules)
                {
                    devSkimRuleSet.AddPath(path);
                }
                var devSkimVerifier = new DevSkimRuleVerifier(new DevSkimRuleVerifierOptions()
                {
                    LanguageSpecs = devSkimLanguages
                    //TODO: Add logging factory to get validation errors.
                });

                var result = devSkimVerifier.Verify(devSkimRuleSet);

                if (!result.Verified)
                {
                    Debug.WriteLine("Error: Rules failed validation. ");
                    return (int)ExitCode.CriticalError;
                }
            }

            if (opts.RuleIds.Any())
            {
                devSkimRuleSet = devSkimRuleSet.WithIds(opts.RuleIds);
            }

            if (opts.IgnoreRuleIds.Any())
            {
                devSkimRuleSet = devSkimRuleSet.WithoutIds(opts.IgnoreRuleIds);
            }
            
            if (!devSkimRuleSet.Any())
            {
                Debug.WriteLine("Error: No rules were loaded. ");
                return (int)ExitCode.CriticalError;
            }

            Severity severityFilter = Severity.Unspecified;
            foreach (var severity in opts.Severities)
            {
                severityFilter |= severity;
            }
            
            Confidence confidenceFilter = Confidence.Unspecified;
            foreach (var confidence in opts.Confidences)
            {
                confidenceFilter |= confidence;
            }

            // Initialize the processor
            var devSkimRuleProcessorOptions = new DevSkimRuleProcessorOptions()
            {
                Languages = devSkimLanguages,
                AllowAllTagsInBuildFiles = true,
                LoggerFactory = NullLoggerFactory.Instance,
                Parallel = !opts.DisableParallel,
                SeverityFilter = severityFilter,
                ConfidenceFilter = confidenceFilter,
            };

            DevSkimRuleProcessor processor = new DevSkimRuleProcessor(devSkimRuleSet, devSkimRuleProcessorOptions);
            processor.EnableSuppressions = !opts.DisableSuppression;
            GitInformation? information = GenerateGitInformation(Path.GetFullPath(opts.Path));
            Writer outputWriter = WriterFactory.GetWriter(string.IsNullOrEmpty(opts.OutputFileFormat) ? "text" : opts.OutputFileFormat,
                                                           opts.OutputTextFormat,
                                                           string.IsNullOrEmpty(opts.OutputFile) ? Console.Out : File.CreateText(opts.OutputFile),
                                                           opts.OutputFile,
                                                           information);

            int filesAnalyzed = 0;
            int filesSkipped = 0;
            int filesAffected = 0;
            int issuesCount = 0;
            void parseFileEntry(FileEntry fileEntry)
            {
                devSkimLanguages.FromFileNameOut(fileEntry.Name, out LanguageInfo languageInfo);

                // Skip files written in unknown language
                if (string.IsNullOrEmpty(languageInfo.Name))
                {
                    Interlocked.Increment(ref filesSkipped);
                }
                else
                {
                    string fileText = string.Empty;

                    try
                    {
                        using (StreamReader reader = new StreamReader(fileEntry.Content))
                        {
                            fileText = reader.ReadToEnd();
                        }
                        Interlocked.Increment(ref filesAnalyzed);
                    }
                    catch (Exception)
                    {
                        // Skip files we can't parse
                        Interlocked.Increment(ref filesSkipped);
                        return;
                    }


                    var issues = processor.Analyze(fileText, fileEntry.Name).ToList();
                    // We need to make sure the issues are ordered by index, so when doing replacements we can keep a straight count of the offset caused by previous changes
                    issues.Sort((issue1, issue2) => issue1.Boundary.Index - issue2.Boundary.Index);

                    bool issuesFound = issues.Any(iss => !iss.IsSuppressionInfo) || opts.DisableSuppression;

                    if (issuesFound)
                    {
                        Interlocked.Increment(ref filesAffected);
                        Debug.WriteLine("file:{0}", fileEntry.FullPath);
                        
                        foreach (Issue issue in issues)
                        {
                            if (!issue.IsSuppressionInfo || opts.DisableSuppression)
                            {
                                Interlocked.Increment(ref issuesCount);
                                Debug.WriteLine("\tregion:{0},{1},{2},{3} - {4} [{5}] - {6}",
                                                        issue.StartLocation.Line,
                                                        issue.StartLocation.Column,
                                                        issue.EndLocation.Line,
                                                        issue.EndLocation.Column,
                                                        issue.Rule.Id,
                                                        issue.Rule.Severity,
                                                        issue.Rule.Name);
                                
                                var record = new DevSkim.IssueRecord(
                                    Filename: TryRelativizePath(opts.BasePath, fileEntry.FullPath),
                                    Filesize: fileText.Length,
                                    TextSample: opts.SkipExcerpts ? string.Empty : fileText.Substring(issue.Boundary.Index, issue.Boundary.Length),
                                    Issue: issue,
                                    Language: languageInfo.Name,
                                    Fixes: issue.Rule.Fixes);
                                
                                outputWriter.WriteIssue(record);
                            }
                        }
                    }
                }
            }

            //Iterate through all files
            if (opts.DisableParallel)
            {
                foreach (var fileEntry in fileListing)
                {
                    parseFileEntry(fileEntry);
                }
            }
            else
            {
                Parallel.ForEach(fileListing, parseFileEntry);
            }

            outputWriter.FlushAndClose();

            Debug.WriteLine("Issues found: {0} in {1} files", issuesCount, filesAffected);
            Debug.WriteLine("Files analyzed: {0}", filesAnalyzed);
            Debug.WriteLine("Files skipped: {0}", filesSkipped);

            return opts.ExitCodeIsNumIssues ? (issuesCount > 0 ? issuesCount : (int)ExitCode.NoIssues) : (int)ExitCode.NoIssues;
        }

        private GitInformation? GenerateGitInformation(string optsPath)
        {
            try
            {
                using var repo = new Repository(optsPath);
                var info = new GitInformation()
                {
                    Branch = repo.Head.FriendlyName
                };
                if (repo.Network.Remotes.Any())
                {
                    info.RepositoryUri = new Uri(repo.Network.Remotes.First().Url);
                }
                if (repo.Head.Commits.Any())
                {
                    info.CommitHash = repo.Head.Commits.First().Sha;
                }

                return info;
            }
            catch
            {
                if (Directory.GetParent(optsPath) is { } notNullParent)
                {
                    return GenerateGitInformation(notNullParent.FullName);
                }
            }

            return null;
        }

        /// <summary>
        /// Open a read stream for the given file name and return a collection with a single file entry representing that file, or an empty collection if the file could not be read
        /// </summary>
        /// <param name="pathToFile"></param>
        /// <returns></returns>
        private static ICollection<FileEntry> FilenameToFileEntryArray(string pathToFile)
        {
            try
            {
                var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read);
                return new FileEntry[] { new FileEntry(pathToFile, fs, null, true) };
            }
            catch (Exception e)
            {
                Debug.WriteLine("The file located at {0} could not be read", pathToFile);
            }
            return Array.Empty<FileEntry>();
        }

        private bool ParseSeverity(string severityText, out Microsoft.ApplicationInspector.RulesEngine.Severity severity)
        {
            severity = Microsoft.ApplicationInspector.RulesEngine.Severity.Critical;
            bool result = true;
            switch (severityText.ToLower())
            {
                case "critical":
                    severity = Microsoft.ApplicationInspector.RulesEngine.Severity.Critical;
                    break;

                case "important":
                    severity = Microsoft.ApplicationInspector.RulesEngine.Severity.Important;
                    break;

                case "moderate":
                    severity = Microsoft.ApplicationInspector.RulesEngine.Severity.Moderate;
                    break;

                case "practice":
                    severity = Microsoft.ApplicationInspector.RulesEngine.Severity.BestPractice;
                    break;

                case "manual":
                    severity = Microsoft.ApplicationInspector.RulesEngine.Severity.ManualReview;
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }
    }
}