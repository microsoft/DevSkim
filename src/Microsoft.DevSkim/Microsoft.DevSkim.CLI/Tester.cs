using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim.CLI
{
    public class Tester
    {
        public Tester(RuleSet rules)
        {            
            _rules = rules;
        }

        public void Run(string directory)
        {
            int totalFiles = 0;
            int failedFiles = 0;
            foreach (string filename in Directory.EnumerateFileSystemEntries(directory, "*.test", SearchOption.AllDirectories))
            {
                totalFiles++;
                failedFiles += (TestFile(filename)) ? 0 : 1;
            }

            Console.WriteLine("Tests: {0}", totalFiles);
            Console.WriteLine("Failed: {0}", failedFiles);
        }

        private bool TestFile(string fileName)
        {
            bool result = true;

            // See if file name is a valid rule ID and preload default values
            string defaultId = Path.GetFileNameWithoutExtension(fileName);
            string[] languages = null;
            Rule fileRule = _rules.FirstOrDefault(x => x.Id == defaultId);
            if (fileRule != null)
                languages = fileRule.AppliesTo;            

            // Load file header and content
            string fileHeader = string.Empty;
            string fileContent = File.ReadAllText(fileName);
            Regex reg = new Regex("^={3,}\\s+", RegexOptions.Multiline);
            Match match = reg.Match(fileContent);
            if (match.Success)
            {
                fileHeader = fileContent.Substring(0, match.Index);
                fileContent = fileContent.Substring(match.Index + match.Length);
            }

            languages = GetLanguges(fileHeader, languages);
            Dictionary<int, List<string>> expecations = GetExpectations(fileHeader, defaultId);

            RuleProcessor processor = new RuleProcessor(_rules);
            processor.EnableSuppressions = false;
            processor.SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice | Severity.ManualReview;

            Issue[] issues = processor.Analyze(fileContent, languages);

            List<KeyValuePair<Location, string>> unexpected = new List<KeyValuePair<Location, string>>();
            
            foreach (Issue issue in issues)
            {
                // if issue on this line was expected remove it from expecations
                int line = issue.Location.Line;
                if (expecations.ContainsKey(line) && expecations[line].Contains(issue.Rule.Id))
                {
                    expecations[line].Remove(issue.Rule.Id);
                }
                // otherwise add it to unexpected
                else
                {
                    unexpected.Add(new KeyValuePair<Location, string>(issue.Location, issue.Rule.Id));
                }
            }
            
            if (unexpected.Count > 0 || expecations.All(x => x.Value.Count > 0))
            {
                result = false;
                Console.WriteLine("file: {0}", fileName);
                foreach(KeyValuePair<Location, string> pair in unexpected)
                {                    
                    Console.WriteLine("\tline:{0},{1} unexpected {2}", pair.Key.Line, pair.Key.Column, pair.Value);
                }

                foreach (int line in expecations.Keys)
                {
                    if (expecations[line].Count > 0)
                    {
                        foreach (string id in expecations[line])
                        {
                            string exists = string.Empty;
                            if (_rules.FirstOrDefault(x => x.Id == id) == null)
                                exists = " (no such rule) ";

                            Console.WriteLine("\tline:{0} expecting {1}{2}", line, id, exists);
                        }
                    }
                }

                Console.WriteLine();
            }

            return result;
        }

        private string[] GetLanguges(string header, string[] defaultLanguages)
        {
            List<string> result = new List<string>();

            Regex reg = new Regex("^language: *(.*)", RegexOptions.Multiline);
            Match match = reg.Match(header);
            if (match.Success)
            {
                result.AddRange(match.Groups[1].Value.Split(',')
                                .Select(x => x.Trim()));                
            }

            if (result.Count() == 0 && defaultLanguages != null)
            {
                result.AddRange(defaultLanguages);
            }

            return result.ToArray();
        }

        private Dictionary<int, List<string>> GetExpectations(string header, string defaultId)
        {
            Dictionary<int, List<string>> result = new Dictionary<int, List<string>>();

            Regex reg = new Regex("^line: *(\\d*)( *expect *)?(.*)", RegexOptions.Multiline);
            MatchCollection matches = reg.Matches(header);
            foreach(Match match in matches)
            {
                int line;
                List<string> ids = new List<string>();
                if (int.TryParse(match.Groups[1].Value, out line))
                {
                    // get list of ids or used default one
                    if (match.Groups[2].Value.Trim() == "expect" && !string.IsNullOrEmpty(match.Groups[3].Value))
                    {
                        ids.AddRange(match.Groups[3].Value.Split(',')
                                     .Select(x => x.Trim()));
                    }
                    else
                    {
                        ids.Add(defaultId);
                    }
                    
                    // Add line and ids to the result set
                    if (result.ContainsKey(line))                    
                        result[line].AddRange(ids);                    
                    else
                        result.Add(line, ids);
                }
            }

            return result;
        }
        
        private RuleSet _rules;
    }
}
