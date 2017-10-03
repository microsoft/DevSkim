using Microsoft.DevSkim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DevSkim.CLI
{
    class Compiler
    {
        public Compiler(string path)
        {
            _messages = new List<ErrorMessage>();
            _rules = new Ruleset();
            _path = path;
        }

        public bool Compile()
        {
            bool isCompiled = true;

            if (string.IsNullOrEmpty(_path))
            {
                Console.Error.WriteLine("Error: Path to rules is missing");
                return false;
            }

            if (File.Exists(_path))
                isCompiled = LoadFile(_path);
            else if (Directory.Exists(_path))
                isCompiled = LoadDirectory(_path);
            else
            {
                Console.Error.WriteLine("Error: Invalid path to rules");
                return false;
            }

            if (isCompiled)
            {
                Verify();
            }

            foreach (ErrorMessage message in _messages)
            {
                Console.Error.WriteLine("{0}: {1}", (message.Warning) ? "Warning" : "Error", message.Message);

                if (!string.IsNullOrEmpty(message.Path))
                    Console.Error.WriteLine("Property: {0}", message.Path);

                if (!string.IsNullOrEmpty(message.RuleID))
                    Console.Error.WriteLine("Rule: {0}", message.RuleID);

                Console.Error.WriteLine("File: {0}", message.File);
                Console.Error.WriteLine();
            }            

            return isCompiled;
        }

        private void Verify()
        {            
            string[] languages = Language.GetNames();

            foreach (Rule rule in _rules.AsEnumerable())
            {
                // Check for null Id
                if (rule.Id == null)
                {
                    _messages.Add(new ErrorMessage()
                    {
                        Message = "Rule has empty ID",
                        Path = rule.Name ?? string.Empty,
                        File = rule.Source,
                        Warning = true
                    });
                }
                else
                {
                    // Check for same ID
                    Rule sameRule = _rules.FirstOrDefault(x => x.Id == rule.Id);
                    if (sameRule != null && sameRule != rule)
                    {
                        _messages.Add(new ErrorMessage()
                        {
                            Message = "Two or more rules have same ID",
                            RuleID = sameRule.Id,
                            File = sameRule.Source,
                            Warning = true
                        });

                        _messages.Add(new ErrorMessage()
                        {
                            Message = "Two or more rules have same ID",
                            RuleID = rule.Id,
                            File = rule.Source,
                            Warning = true
                        });
                    }
                }

                if (rule.AppliesTo != null)
                {
                    // Check for unknown language
                    foreach (string lang in rule.AppliesTo)
                    {
                        if (!languages.Contains(lang))
                        {
                            _messages.Add(new ErrorMessage()
                            {
                                Message = string.Format("Unknown language '{0}'", lang),
                                RuleID = rule.Id ?? string.Empty,
                                Path = "applies_to",
                                File = rule.Source,
                                Warning = true
                            });
                        }
                    }
                }
            }
        }

        private bool LoadDirectory(string path)
        {
            bool result = true;
            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                if (!LoadFile(filename))
                    result = false;
            }

            return result;
        }

        private bool LoadFile(string file)
        {
            Ruleset rules = new Ruleset();
            bool noProblem = true;
            rules.OnDeserializationError += delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
            {
                ErrorMessage message = new ErrorMessage()
                {
                    File = file,
                    Message = e.ErrorContext.Error.Message,
                    Path = e.ErrorContext.Path
                };

                if (e.ErrorContext.OriginalObject is Rule r && !string.IsNullOrEmpty(r.Id))
                {
                    message.RuleID = r.Id;
                }

                // Newtonsoft json throws some errors twice
                if (_messages.FirstOrDefault(x => (x.Message == message.Message && x.File == file)) == null)
                    _messages.Add(message);

                noProblem = false;
                e.ErrorContext.Handled = true;
            };

            rules.AddFile(file, null);

            if (noProblem)
                _rules.AddRange(rules.AsEnumerable());

            return noProblem;
        }

        public ErrorMessage[] Messages
        {
            get { return _messages.ToArray(); }
        }

        public Ruleset CompiledRuleset
        {
            get { return _rules; }
        }

        private List<ErrorMessage> _messages = new List<ErrorMessage>();
        private Ruleset _rules;
        private string _path;
    }
}
