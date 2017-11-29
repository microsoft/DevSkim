// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class AnalyzeCommand : ICommand
    {
        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Analyze source code";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to source code");

            var outputArgument = command.Argument("[output]",
                                                  "Output file");

            /*
            var outputFileFormat = command.Option("[-o|--fileformat]",
                                                  "Output file format\njson",
                                                  CommandOptionType.SingleValue);*/            

            var severityOption = command.Option("-s|--severity",
                                                "Severity: [critical,important,moderate,practice,review]",
                                                CommandOptionType.MultipleValue);

            var rulesOption = command.Option("-r|--rules",
                                             "Rules to use",
                                             CommandOptionType.MultipleValue);

            var ignoreOption = command.Option("-i|--ignore-default-rules",
                                              "Ignore rules bundled with DevSkim",
                                              CommandOptionType.NoValue);

            command.OnExecute(() => {
                return (new AnalyzeCommand(locationArgument.Value,
                                 outputArgument.Value,
                                 severityOption.Values,
                                 rulesOption.Values,
                                 ignoreOption.HasValue())).Run();                
            });
        }

        public AnalyzeCommand(string path, 
                              string output,
                              List<string> severities,
                              List<string> rules,
                              bool ignoreDefault)
        {
            _path = path;            
            _outputfile = output;
            _severities = severities.ToArray();
            _rulespath = rules.ToArray();
            _ignoreDefaultRules = ignoreDefault;
        }

        public int Run()
        {
            if (!Directory.Exists(_path) && !File.Exists(_path))
            {
                Console.Error.WriteLine("Error: Not a valid file or directory {0}", _path);                
                return 2;
            }

            Verifier verifier = null;
            if (_rulespath.Count() > 0)
            {
                // Setup the rules
                verifier = new Verifier(_rulespath);
                if (!verifier.Verify())
                    return 2;

                if (verifier.CompiledRuleset.Count() == 0 && _ignoreDefaultRules)
                {
                    Console.Error.WriteLine("Error: No rules were loaded. ");
                    return 2;
                }
            }

            RuleSet rules = new RuleSet();
            if (verifier != null)
                rules = verifier.CompiledRuleset;

            if (!_ignoreDefaultRules)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string filePath = "Microsoft.DevSkim.CLI.Resources.devskim-rules.json";
                Stream resource = assembly.GetManifestResourceStream(filePath);
                using (StreamReader file = new StreamReader(resource))
                {
                    rules.AddString(file.ReadToEnd(), filePath, null);
                }                
            }

            // Initialize the processor
            RuleProcessor processor = new RuleProcessor(rules);

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
                        Console.WriteLine("Invalid severity: {0}", severityText);
                        return 2;
                    }
                }
            }

            // Store the results here (JSON only)
            var jsonResult = new List<Dictionary<string, string>>();

            int filesAnalyzed = 0;
            int filesSkipped = 0;
            int filesAffected = 0;
            int issuesCount = 0;

            // Iterate through all files
            foreach (string filename in Directory.EnumerateFiles(_path, "*.*", SearchOption.AllDirectories))
            {
                string language = Language.FromFileName(filename);

                // Skip files written in unknown language
                if (string.IsNullOrEmpty(language))
                {
                    filesSkipped++;
                    continue;
                }

                filesAnalyzed++;
                string fileText = File.ReadAllText(filename);
                Issue[] issues = processor.Analyze(fileText, language);

                if (issues.Count() > 0)
                {
                    filesAffected++;
                    issuesCount += issues.Count();
                    Console.WriteLine("file:{0}", filename);

                    // Iterate through each issue
                    foreach (Issue issue in issues)
                    {
                        if (string.IsNullOrEmpty(_outputfile))
                        {
                            Console.WriteLine("\tline:{0},{1} - {2} [{3}] - {4}",                                                          
                                                          issue.Location.Line,
                                                          issue.Location.Column,
                                                          issue.Rule.Id,
                                                          issue.Rule.Severity,
                                                          issue.Rule.Name);
                        }
                        else
                        {
                            // Store the result in the result list
                            jsonResult.Add(new Dictionary<string, string>()
                        {
                            { "filename", filename },
                            { "line_number", issue.Location.Line.ToString() },
                            { "line_position", issue.Location.Column.ToString() },
                            { "matching_section", fileText.Substring(issue.Boundary.Index, issue.Boundary.Length) },
                            { "rule_id", issue.Rule.Id },
                            { "rule_name", issue.Rule.Name },
                            { "rule_description", issue.Rule.Description }
                        });
                        }
                    }

                    if (string.IsNullOrEmpty(_outputfile))
                        Console.WriteLine();
                }
            }

            if (!string.IsNullOrEmpty(_outputfile))
            {
                File.WriteAllText(_outputfile, JsonConvert.SerializeObject(jsonResult, Formatting.Indented));
            }

            Console.WriteLine("Issues found: {0} in {1} files", issuesCount, filesAffected);
            Console.WriteLine("Files analyzed: {0}", filesAnalyzed);
            Console.WriteLine("Files skipped: {0}", filesSkipped);

            return 0;
        }

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

        private string _path;
        private string _outputfile;
        private string[] _rulespath;
        private string[] _severities;
        private bool _ignoreDefaultRules;
    }
}