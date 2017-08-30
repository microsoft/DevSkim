using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.DevSkim;

namespace Microsoft.DevSkim.CLI
{
    class Program
    {
        private static string pathToScan;
        private static string customRulesPath;
        private static string outputFormat;
        private static string textOutputFormat = "%f:%l [%n] %z";

        static void Main(string[] args)
        {
            // Parse Arguments
            for (var i=0; i<args.Length; i++)
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
                    System.Environment.Exit(1);
                }

                pathToScan = args[i];
            }

            if (!Directory.Exists(pathToScan) && !File.Exists(pathToScan))
            {
                Console.Error.WriteLine("Path or file specified does not exist.");
                ShowUsage();
                System.Environment.Exit(1);
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
                System.Environment.Exit(1);
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
                foreach (var issue in processor.Analyze(fileText, Language.FromFileName(filename)))
                {
                    // Get the line number based on the issue's index (byte offset)
                    int lineNumber = fileText.Substring(0, issue.Index).Count(s => s == '\n') + 1;

                    if (outputFormat == "json")
                    {
                        // Store the result in the result list
                        jsonResult.Add(new Dictionary<string, string>()
                        {
                            { "filename", filename },
                            { "line_number", Convert.ToString(lineNumber) },
                            { "line", GetLineFromLineNumber(fileText, lineNumber, 100) },
                            { "matching_section", fileText.Substring(issue.Index, issue.Length).Trim() },
                            { "rule_name", issue.Rule.Name },
                            { "rule_description", issue.Rule.Description }
                        });
                    }
                    else
                    {
                        string output = textOutputFormat;
                        output = output.Replace("%f", filename);
                        output = output.Replace("%l", Convert.ToString(lineNumber));
                        output = output.Replace("%i", Convert.ToString(issue.Index));
                        output = output.Replace("%j", Convert.ToString(issue.Length));
                        output = output.Replace("%n", issue.Rule.Name);
                        output = output.Replace("%d", issue.Rule.Description);
                        output = output.Replace("%s", fileText.Substring(issue.Index, issue.Length).Trim());
                        output = output.Replace("%z", GetLineFromLineNumber(fileText, lineNumber, 100));
                        Console.WriteLine(output);
                    }
                }
            }
            if (outputFormat == "json")
            {
                Console.Write(JsonConvert.SerializeObject(jsonResult, Formatting.Indented));
            }
        }

        /**
         * Return the entire line from a string based on the line number given. Newlines
         * are separated by '\n'.
         * This function is not optimized for large files.
         * If maxLength is specified, then the line will be cut to that max length.
         */
        static string GetLineFromLineNumber(string text, int lineNumber, int maxLength=-1)
        {
            var s = text.Split(new char[] { '\n' })[lineNumber - 1];
            s = s.Trim();
            if (maxLength == -1 || maxLength > s.Length)
            {
                return s;
            }
            else
            {
                return s.Substring(0, Math.Min(s.Length, maxLength)).Trim();
            }
        }
        
        static void ShowUsage()
        {
            Console.Error.WriteLine("Usage: DevSkim.exe [--custom-rules PATH] [--output-format (text|json)] [--text-format-specifier SPECIFIER] PATH-TO-SCAN");
            Console.Error.WriteLine("See the wiki for details.");
        }
        
    }
}
