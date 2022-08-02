// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using GlobExpressions;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.DevSkim.CLI.Writers;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim.AI;
using Microsoft.DevSkim.AI.CLI.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DevSkim.AI.CLI.Commands
{
    public class AnalyzeCommand : ICommand
    {
        private AnalyzeCommandOptions opts;

        public AnalyzeCommand(AnalyzeCommandOptions options)
        {
            opts = options;
        }

        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Analyze source code";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to source code");

            var outputArgument = command.Argument("[output]",
                                                  "Output file");

            var outputFileFormat = command.Option("-f|--file-format",
                                                  "Output file format: [text,json,sarif]",
                                                  CommandOptionType.SingleValue);

            var outputTextFormat = command.Option("-o|--output-format",
                                                  "Output format for text writer or elements to include for json writer.",
                                                  CommandOptionType.SingleValue);

            var severityOption = command.Option("-s|--severity",
                                                "Severity: [critical,important,moderate,practice,manual]",
                                                CommandOptionType.SingleValue);

            var globOptions = command.Option("-g|--ignore-globs",
                                    "**/.git/**,**/bin/**",
                                    CommandOptionType.SingleValue);

            var disableSuppressionOption = command.Option("-d|--disable-suppression",
                                                   "Disable suppression of findings with ignore comments",
                                                   CommandOptionType.NoValue);

            var disableParallel = command.Option("--disable-parallel",
                                       "Disable parallel processing.",
                                       CommandOptionType.NoValue);

            var rulesOption = command.Option("-r|--rules",
                                             "Rules to use. Comma delimited.",
                                             CommandOptionType.SingleValue);

            var ignoreOption = command.Option("-i|--ignore-default-rules",
                                              "Ignore rules bundled with DevSkim",
                                              CommandOptionType.NoValue);

            var errorOption = command.Option("-e|--suppress-standard-error",
                                              "Suppress output to standard error",
                                              CommandOptionType.NoValue);

            var crawlArchives = command.Option("-c|--crawl-archives",
                                       "Enable crawling into archives when processing directories.",
                                       CommandOptionType.NoValue);

            var exitCodeIsNumIssues = command.Option("-E",
                                        "Use the exit code to indicate number of issues identified.",
                                        CommandOptionType.NoValue);

            var basePath = command.Option("--base-path",
                                        "Specify what path to root result URIs with. When not set will generate paths relative to the source directory (or directory containing the source file specified).",
                                        CommandOptionType.SingleValue);

            var absolutePaths = command.Option("--absolute-path",
                           "Output absolute paths (overrides --base-path).",
                           CommandOptionType.NoValue);

            command.ExtendedHelpText = 
@"
Output format options:
%F  file path
%L  start line number
%C  start column
%l  end line number
%c  end column
%I  location inside file
%i  match length
%m  match
%R  rule id
%N  rule name
%S  severity
%D  issue description
%T  tags (comma-separated in text writer)
%f  fixes (json only)";

            command.OnExecute((Func<int>)(() =>
            {
                var opts = new AnalyzeCommandOptions()
                {
                    Path = locationArgument.Value,
                    OutputFile = outputArgument.Value,
                    OutputFileFormat = outputFileFormat.Value(),
                    OutputTextFormat = outputTextFormat.Value(),
                    Severities = severityOption.Value()?.Split(',') ?? Array.Empty<string>(),
                    Rulespath = rulesOption.Value()?.Split(',') ?? Array.Empty<string>(),
                    IgnoreDefaultRules = ignoreOption.HasValue(),
                    SuppressError = errorOption.HasValue(),
                    DisableSuppression = disableSuppressionOption.HasValue(),
                    CrawlArchives = crawlArchives.HasValue(),
                    ExitCodeIsNumIssues = exitCodeIsNumIssues.HasValue(),
                    Globs = globOptions.Value()?.Split(',').Select<string, Glob>(x => new Glob(x)) ?? Array.Empty<Glob>(),
                    BasePath = basePath.Value(),
                    DisableParallel = disableParallel.HasValue(),
                    AbsolutePaths = absolutePaths.HasValue()
                };
                return (new AnalyzeCommand(opts).Run());
            }));
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

            IEnumerable<FileEntry> fileListing;
            var extractor = new Extractor();
            var fp = Path.GetFullPath(opts.Path);
            if (!Directory.Exists(fp))
            {
                fileListing = extractor.Extract(fp, new ExtractorOptions() { ExtractSelfOnFail = false, DenyFilters = opts.Globs.Select(x => x.Pattern)});
            }
            else
            {
                fileListing = Directory.EnumerateFiles(fp, "*.*", SearchOption.AllDirectories).Where(x => !opts.Globs.Any(y => y.IsMatch(x))).SelectMany(x => opts.CrawlArchives ? extractor.Extract(x, new ExtractorOptions() { ExtractSelfOnFail = false, DenyFilters = opts.Globs.Select(x => x.Pattern) }) : FilenameToFileEntryArray(x));
            }
            return RunFileEntries(fileListing);
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

        public int RunFileEntries(IEnumerable<FileEntry> fileListing, StreamWriter? outputStreamWriter = null)
        {
            DevSkimRuleSet devSkimRuleSet = opts.IgnoreDefaultRules ? new() : DevSkimRuleSet.GetDefaultRuleSet();
            Languages devSkimLanguages = DevSkimLanguages.LoadEmbedded();
            if (opts.Rulespath.Length > 0)
            {
                foreach (var path in opts.Rulespath)
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
            
            if (!devSkimRuleSet.Any())
            {
                Debug.WriteLine("Error: No rules were loaded. ");
                return (int)ExitCode.CriticalError;
            }
            
            // Initialize the processor
            var devSkimRuleProcessorOptions = new DevSkimRuleProcessorOptions()
            {
                Languages = devSkimLanguages,
                AllowAllTagsInBuildFiles = true,
                LoggerFactory = NullLoggerFactory.Instance,
                Parallel = !opts.DisableParallel
                // TODO: Parse command line options into appropriate AI options
            };

            if (opts.Severities.Count() > 0)
            {
                devSkimRuleProcessorOptions.SeverityFilter = 0;
                foreach (string severityText in opts.Severities)
                {
                    if (ParseSeverity(severityText, out Microsoft.ApplicationInspector.RulesEngine.Severity severity))
                    {
                        devSkimRuleProcessorOptions.SeverityFilter |= severity;
                    }
                    else
                    {
                        Debug.WriteLine("Invalid severity: {0}", severityText);
                        return (int)ExitCode.CriticalError;
                    }
                }
            }

            DevSkimRuleProcessor processor = new DevSkimRuleProcessor(devSkimRuleSet, devSkimRuleProcessorOptions);
            processor.EnableSuppressions = !opts.DisableSuppression;
            
            Writer outputWriter = WriterFactory.GetWriter(string.IsNullOrEmpty(opts.OutputFileFormat) ? "text" : opts.OutputFileFormat,
                                                           opts.OutputTextFormat,
                                                           (outputStreamWriter is null)?(string.IsNullOrEmpty(opts.OutputFile) ? Console.Out : File.CreateText(opts.OutputFile)):outputStreamWriter,
                                                           (outputStreamWriter is null)?opts.OutputFile:null);

            int filesAnalyzed = 0;
            int filesSkipped = 0;
            int filesAffected = 0;
            int issuesCount = 0;
            var Languages = new Languages();
            void parseFileEntry(FileEntry fileEntry)
            {
                Uri baseUri = new Uri(Path.GetFullPath(opts.Path));
                Languages.FromFileNameOut(fileEntry.Name, out LanguageInfo languageInfo);

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

                    bool issuesFound = issues.Any(iss => !iss.IsSuppressionInfo) || opts.DisableSuppression && issues.Any();

                    if (issuesFound)
                    {
                        Interlocked.Increment(ref filesAffected);
                        Debug.WriteLine("file:{0}", fileEntry.FullPath);

                        // Iterate through each issue
                        foreach (Microsoft.DevSkim.AI.Issue issue in issues)
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
                                
                                var record = new AI.IssueRecord(
                                    Filename: TryRelativizePath(opts.BasePath, fileEntry.FullPath),
                                    Filesize: fileText.Length,
                                    TextSample: fileText.Substring(issue.Boundary.Index, issue.Boundary.Length),
                                    Issue: issue,
                                    Language: languageInfo.Name);
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

        private IEnumerable<FileEntry> FilenameToFileEntryArray(string x)
        {
            try
            {
                var fs = new FileStream(x, FileMode.Open, FileAccess.Read);
                return new FileEntry[] { new FileEntry(x, fs, null, true) };
            }
            catch (Exception) { }
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