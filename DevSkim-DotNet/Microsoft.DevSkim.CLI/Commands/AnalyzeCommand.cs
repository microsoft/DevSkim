// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

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

namespace Microsoft.DevSkim.CLI.Commands
{
    public class AnalyzeCommand : ICommand
    {
        public AnalyzeCommand(string path,
                              string output,
                              string outputFileFormat,
                              string outputTextFormat,
                              List<string> severities,
                              List<string> rules,
                              bool ignoreDefault,
                              bool suppressError,
                              bool disableSuppression,
                              bool crawlArchives,
                              bool disableParallel)
        {
            _path = path;
            _outputFile = output;
            _fileFormat = outputFileFormat;
            _outputFormat = outputTextFormat;
            _severities = severities.ToArray();
            _rulespath = rules.ToArray();
            _ignoreDefaultRules = ignoreDefault;
            _suppressError = suppressError;
            _disableSuppression = disableSuppression;
            _crawlArchives = crawlArchives;
            _disableParallel = disableParallel;
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
                                                  "Output text format",
                                                  CommandOptionType.SingleValue);

            var severityOption = command.Option("-s|--severity",
                                                "Severity: [critical,important,moderate,practice,manual]",
                                                CommandOptionType.MultipleValue);

            var disableSuppressionOption = command.Option("-d|--disable-suppression",
                                                   "Disable suppression of findings with ignore comments",
                                                   CommandOptionType.NoValue);

            var disableParallel = command.Option("--disable-parallel",
                                       "Disable parallel processing.",
                                       CommandOptionType.NoValue);

            var rulesOption = command.Option("-r|--rules",
                                             "Rules to use",
                                             CommandOptionType.MultipleValue);

            var ignoreOption = command.Option("-i|--ignore-default-rules",
                                              "Ignore rules bundled with DevSkim",
                                              CommandOptionType.NoValue);

            var errorOption = command.Option("-e|--suppress-standard-error",
                                              "Suppress output to standard error",
                                              CommandOptionType.NoValue);

            var crawlArchives = command.Option("-c|--crawl-archives",
                                       "Enable crawling into archives when processing directories.",
                                       CommandOptionType.NoValue);

            command.ExtendedHelpText = "\nOutput format options:\n%F\tfile path\n%L\tstart line number\n" +
                "%C\tstart column\n%l\tend line number\n%c\tend column\n%I\tlocation inside file\n" +
                "%i\tmatch length\n%m\tmatch\n%R\trule id\n%N\trule name\n%S\tseverity\n%D\tissue description\n%T\ttags(comma-separated)";

            command.OnExecute(() =>
            {
                return (new AnalyzeCommand(locationArgument.Value,
                                 outputArgument.Value,
                                 outputFileFormat.Value(),
                                 outputTextFormat.Value(),
                                 severityOption.Values,
                                 rulesOption.Values,
                                 ignoreOption.HasValue(),
                                 errorOption.HasValue(),
                                 disableSuppressionOption.HasValue(),
                                 crawlArchives.HasValue(),
                                 disableParallel.HasValue())).Run();
            });
        }

        public int Run()
        {
            if (_suppressError)
            {
                Console.SetError(StreamWriter.Null);
            }

            if (!Directory.Exists(_path) && !File.Exists(_path))
            {
                Debug.WriteLine("Error: Not a valid file or directory {0}", _path);

                return (int)ExitCode.CriticalError;
            }

            IEnumerable<FileEntry> fileListing;
            var extractor = new Extractor();

            if (!Directory.Exists(_path))
            {
                fileListing = extractor.Extract(_path, new ExtractorOptions() { ExtractSelfOnFail = false });
            }
            else
            {
                fileListing = Directory.EnumerateFiles(_path, "*.*", SearchOption.AllDirectories).SelectMany(x => _crawlArchives ? extractor.Extract(x, new ExtractorOptions() { ExtractSelfOnFail = false }) : FilenameToFileEntryArray(x));
            }
            return RunFileEntries(fileListing);
        }

        public int RunFileEntries(IEnumerable<FileEntry> fileListing, StreamWriter? outputStreamWriter = null)
        {
            Verifier? verifier = null;
            if (_rulespath.Length > 0)
            {
                // Setup the rules
                verifier = new Verifier(_rulespath);
                if (!verifier.Verify())
                    return (int)ExitCode.CriticalError;

                if (verifier.CompiledRuleset.Count() == 0 && _ignoreDefaultRules)
                {
                    Debug.WriteLine("Error: No rules were loaded. ");
                    return (int)ExitCode.CriticalError;
                }
            }

            RuleSet rules = new RuleSet();
            if (verifier != null)
                rules = verifier.CompiledRuleset;

            if (!_ignoreDefaultRules)
            {
                Assembly? assembly = Assembly.GetAssembly(typeof(Boundary));
                string filePath = "Microsoft.DevSkim.Resources.devskim-rules.json";
                Stream? resource = assembly?.GetManifestResourceStream(filePath);
                if (resource is Stream)
                {
                    using (StreamReader file = new StreamReader(resource))
                    {
                        var rulesString = file.ReadToEnd();
                        rules.AddString(rulesString, filePath, null);
                    }
                }
            }

            // Initialize the processor
            RuleProcessor processor = new RuleProcessor(rules);
            processor.EnableSuppressions = !_disableSuppression;

            if (_severities.Count() > 0)
            {
                processor.SeverityLevel = 0;
                foreach (string severityText in _severities)
                {
                    Severity severity;
                    if (ParseSeverity(severityText, out severity))
                    {
                        processor.SeverityLevel |= severity;
                    }
                    else
                    {
                        Debug.WriteLine("Invalid severity: {0}", severityText);
                        return (int)ExitCode.CriticalError;
                    }
                }
            }

            Writer outputWriter = WriterFactory.GetWriter(string.IsNullOrEmpty(_fileFormat) ? "text" : _fileFormat,
                                                           _outputFormat,
                                                           (outputStreamWriter is null)?(string.IsNullOrEmpty(_outputFile) ? Console.Out : File.CreateText(_outputFile)):outputStreamWriter,
                                                           (outputStreamWriter is null)?_outputFile:null);

            int filesAnalyzed = 0;
            int filesSkipped = 0;
            int filesAffected = 0;
            int issuesCount = 0;

            void parseFileEntry(FileEntry fileEntry)
            {
                string language = Language.FromFileName(fileEntry.FullPath);

                // Skip files written in unknown language
                if (string.IsNullOrEmpty(language))
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

                    Issue[] issues = processor.Analyze(fileText, language);

                    bool issuesFound = issues.Any(iss => !iss.IsSuppressionInfo) || _disableSuppression && issues.Any();

                    if (issuesFound)
                    {
                        Interlocked.Increment(ref filesAffected);
                        Debug.WriteLine("file:{0}", fileEntry.FullPath);

                        // Iterate through each issue
                        foreach (Issue issue in issues)
                        {
                            if (!issue.IsSuppressionInfo || _disableSuppression)
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

                                IssueRecord record = new IssueRecord(
                                    Filename: fileEntry.FullPath,
                                    Filesize: fileText.Length,
                                    TextSample: fileText.Substring(issue.Boundary.Index, issue.Boundary.Length),
                                    Issue: issue,
                                    Language: language);

                                outputWriter.WriteIssue(record);
                            }
                        }
                    }
                }
            }

            //Iterate through all files
            if (_disableParallel)
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

            return (int)ExitCode.NoIssues;
        }

        private IEnumerable<FileEntry> FilenameToFileEntryArray(string x)
        {
            try
            {
                var fs = new FileStream(x, FileMode.Open, FileAccess.ReadWrite);
                return new FileEntry[] { new FileEntry(x, fs, null, true) };
            }
            catch (Exception) { }
            return Array.Empty<FileEntry>();
        }

        private readonly bool _crawlArchives;
        private readonly bool _disableParallel;
        private bool _disableSuppression;

        private string _fileFormat;

        private bool _ignoreDefaultRules;

        private string _outputFile;

        private string _outputFormat;

        private string _path;

        private string[] _rulespath;

        private string[] _severities;

        private bool _suppressError;

        private bool ParseSeverity(string severityText, out Severity severity)
        {
            severity = Severity.Critical;
            bool result = true;
            switch (severityText.ToLower())
            {
                case "critical":
                    severity = Severity.Critical;
                    break;

                case "important":
                    severity = Severity.Important;
                    break;

                case "moderate":
                    severity = Severity.Moderate;
                    break;

                case "practice":
                    severity = Severity.BestPractice;
                    break;

                case "manual":
                    severity = Severity.ManualReview;
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }
    }
}