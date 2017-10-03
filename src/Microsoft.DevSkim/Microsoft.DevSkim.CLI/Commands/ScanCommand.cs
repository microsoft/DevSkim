using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class ScanCommand : ICommand
    {
        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Scan source code";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to source code");

            var outputArgument = command.Argument("[output]",
                                                    "Output file");

            var rulesOption = command.Option("-r|--rules",
                                              "Rules to use",
                                              CommandOptionType.MultipleValue);

            command.OnExecute(() => {
                return (new ScanCommand(locationArgument.Value,
                                 outputArgument.Value,
                                 rulesOption.Values)).Run();                
            });
        }

        public ScanCommand(string path, string output, List<string> rules)
        {
            _path = path;
            _outputfile = output;
            _rulespath = rules.ToArray();
        }

        public int Run()
        {
            if (!Directory.Exists(_path) && !File.Exists(_path))
            {
                Console.Error.WriteLine("Error: Path or file specified does not exist.");                
                return 1;
            }

            // Set up the rules
            Ruleset rules = new Ruleset();
            foreach (string rulesPath in _rulespath)
            {
                if (Directory.Exists(rulesPath))
                    rules.AddDirectory(rulesPath, null);
                else if (File.Exists(rulesPath))
                    rules.AddFile(rulesPath, null);
                else
                    Console.Error.WriteLine("Warning: Path {0} does not exists", rulesPath);
            }

            if (rules.Count() == 0)
            {
                Console.Error.WriteLine("Error: No rules were loaded. ");                
                return 1;
            }

            // Initialize the processor
            RuleProcessor processor = new RuleProcessor(rules);

            // Store the results here (JSON only)
            var jsonResult = new List<Dictionary<string, string>>();

            // Iterate through all files
            foreach (string filename in Directory.EnumerateFiles(_path, "*.*", SearchOption.AllDirectories))
            {
                string language = Language.FromFileName(filename);

                // Skip files written in unknown language
                if (string.IsNullOrEmpty(language))
                    continue;

                string fileText = File.ReadAllText(filename);

                // Iterate through each issue
                foreach (Issue issue in processor.Analyze(fileText, language))
                {
                    if (string.IsNullOrEmpty(_outputfile))
                    {
                        Console.WriteLine(string.Format("{0}:{1},{2} {3} {4}",
                                                      filename,
                                                      issue.Location.Line,
                                                      issue.Location.Column,
                                                      issue.Rule.Id,
                                                      issue.Rule.Name));                        
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
            }

            if (!string.IsNullOrEmpty(_outputfile))
            {
                File.WriteAllText(_outputfile, JsonConvert.SerializeObject(jsonResult, Formatting.Indented));
            }

            return 0;
        }

        private string _path;
        private string _outputfile;
        private string[] _rulespath;
    }
}