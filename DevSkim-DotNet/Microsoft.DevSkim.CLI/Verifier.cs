﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DevSkim.CLI
{
    internal class Verifier
    {
        public Verifier(string[] paths)
        {
            _messages = new List<ErrorMessage>();
            _rules = new RuleSet();
            _paths = paths;
        }

        public Verifier(string path)
            : this(new string[] { path })
        {
        }

        public RuleSet CompiledRuleset
        {
            get { return _rules; }
        }

        public ErrorMessage[] Messages
        {
            get { return _messages.ToArray(); }
        }

        public bool Verify()
        {

            foreach (string rulesPath in _paths)
            {
                if (Directory.Exists(rulesPath))
                    LoadDirectory(rulesPath);
                else if (File.Exists(rulesPath))
                    LoadFile(rulesPath);
                else
                    Console.Error.WriteLine("Error: Not a valid file or directory {0}", rulesPath);
            }

            CheckIntegrity();

            foreach (ErrorMessage message in _messages)
            {
                Console.Error.WriteLine("file:{0}", message.File);

                if (!string.IsNullOrEmpty(message.Path))
                    Console.Error.WriteLine("\tproperty: {0}", message.Path);

                if (!string.IsNullOrEmpty(message.RuleID))
                    Console.Error.WriteLine("\trule: {0}", message.RuleID);

                Console.Error.WriteLine("\tseverity: {0}", (message.Warning) ? "warning" : "error");
                Console.Error.WriteLine("\tmessage: {0}", message.Message);

                Console.Error.WriteLine();
            }

            return !_messages.Any(x => x.Warning == false);
        }

        private List<ErrorMessage> _messages = new List<ErrorMessage>();

        private string[] _paths;

        private RuleSet _rules;

        private void CheckIntegrity()
        {
            string[] languages = Language.GetNames();

            foreach (Rule rule in _rules.AsEnumerable().Select(x => x.DevSkimRule))
            {
                // Check for null Id
                if (rule.Id == null)
                {
                    _messages.Add(new ErrorMessage(Message: "Rule has empty ID", Path: rule.Name, File: rule.Source, Warning: true));
                }
                else
                {
                    // Check for same ID
                    Rule? sameRule = _rules.Select(x => x.DevSkimRule).FirstOrDefault(x => x.Id == rule.Id);
                    if (sameRule is { } && _rules.Count(x => x.DevSkimRule.Id == rule.Id) > 1)
                    {
                        _messages.Add(new ErrorMessage(Message: "Two or more rules have a same ID", RuleID: sameRule.Id, File: sameRule.Source, Warning: true));
                        _messages.Add(new ErrorMessage(Message: "Two or more rules have a same ID", RuleID: rule.Id, File: rule.Source, Warning: true));
                    }

                }

                if (rule.AppliesTo != null)
                {
                    // Check for unknown language
                    foreach (string lang in rule.AppliesTo)
                    {
                        if (!languages.Contains(lang))
                        {
                            _messages.Add(new ErrorMessage(Message: string.Format("Unknown language '{0}'", lang),
                                RuleID: rule.Id ?? string.Empty,
                                Path: "applies_to",
                                File: rule.Source,
                                Warning: true));
                        }
                    }
                }
                if (rule.DoesNotApplyTo != null)
                {
                    foreach(string lang in rule.DoesNotApplyTo){
                        if (!languages.Contains(lang)){
                            _messages.Add(new ErrorMessage(Message: string.Format("Unknown language '{0}'", lang),
                                RuleID: rule.Id ?? string.Empty,
                                Path: "does_not_apply_to",
                                File: rule.Source,
                                Warning: true));
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
            RuleSet rules = new RuleSet();
            bool noProblem = true;

            try
            {
                rules.AddFile(file, null);
                _rules.AddRange(rules.AsEnumerable().Select(x => x.DevSkimRule));
            }
            catch(Exception e)
            {
                noProblem = false;
                ErrorMessage message = new ErrorMessage(File: file, Message: e.Message);
            }
            return noProblem;
        }
    }
}