using System;
using Newtonsoft.Json;
using Microsoft.DevSkim;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.DevSkim.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            string pathToScan = null;
            string customRulesPath = null;
            string outputFormat = null;
            string textOutputFormat = "%f:%l [%n] %s";
            
            // Parse Arguments
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--add-rules")
                {
                    customRulesPath = args[++i];
                    continue;
                }
                if (args[i] == "--output-format")
                {
                    outputFormat = args[++i];
                    continue;
                }
                if (args[i] == "--text-format-specifier")
                {
                    textOutputFormat = args[++i];
                    continue;
                }
                if (args[i] == "--help" || args[i] == "/help" || args[i] == "/?")
                {
                    ShowUsage();
                    return 1;
                }

                pathToScan = args[i];
            }

            if (!Directory.Exists(pathToScan) && !File.Exists(pathToScan))
            {
                Console.Error.WriteLine("Path or file specified does not exist.");
                ShowUsage();
                return 1;
            }

            // Set up the rules
            Ruleset rules = new Ruleset();
            if (Directory.Exists("rules"))
            {
                rules.AddDirectory("rules", null);
            }

            if (customRulesPath != null)
            {
                rules.AddDirectory(customRulesPath, "custom");
            }

            if (rules.Count() == 0)
            {
                Console.Error.WriteLine("No rules found. Either pass --add-rules or ensure that directory 'rules' exists.");
                ShowUsage();
                return 1;
            }

            // Initialize the processor
            RuleProcessor processor = new RuleProcessor(rules);

            // Store the results here (JSON only)
            var jsonResult = new List<Dictionary<string, string>>();

            // Iterate through all files
            foreach (string filename in Directory.EnumerateFiles(pathToScan, "*.*", SearchOption.AllDirectories))
            {
                var fileText = File.ReadAllText(filename);

                // Iterate through each rule
                foreach (Issue issue in processor.Analyze(fileText, Language.FromFileName(filename)))
                {
                    if (outputFormat == "json")
                    {
                        // Store the result in the result list
                        jsonResult.Add(new Dictionary<string, string>()
                        {
                            { "filename", filename },
                            { "line_number", issue.Location.Line.ToString() },                            
                            { "matching_section", fileText.Substring(issue.Boundary.Index, issue.Boundary.Length) },
                            { "rule_name", issue.Rule.Name },
                            { "rule_description", issue.Rule.Description }
                        });
                    }
                    else
                    {
                        string output = textOutputFormat;
                        output = output.Replace("%f", filename);
                        output = output.Replace("%l", issue.Location.Line.ToString());
                        output = output.Replace("%i", issue.Boundary.Index.ToString());
                        output = output.Replace("%j", issue.Boundary.Length.ToString());
                        output = output.Replace("%n", issue.Rule.Name);
                        output = output.Replace("%d", issue.Rule.Description);
                        output = output.Replace("%s", fileText.Substring(issue.Boundary.Index, issue.Boundary.Length).Trim());                        
                        Console.WriteLine(output);
                    }
                }
            }
            if (outputFormat == "json")
            {
                Console.Write(JsonConvert.SerializeObject(jsonResult, Formatting.Indented));
            }

            return 0;
        }

        static void ShowUsage()
        {
            Console.Error.WriteLine("Usage: DevSkim.exe [--custom-rules PATH] [--output-format (text|json)] [--text-format-specifier SPECIFIER] PATH-TO-SCAN");
            Console.Error.WriteLine("See the wiki for details.");            
        }
    }
}
