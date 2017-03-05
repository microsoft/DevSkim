// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Microsoft.Security.DevSkim
{
    /// <summary>
    /// Provides functionality for loading the rules
    /// </summary>
    public class Ruleset
    {
        private Ruleset()
        {
            _rules = new List<Rule>();
        }

        /// <summary>
        /// Parse a directory with rules files and loads the rules
        /// </summary>
        /// <param name="path">Path to rules folder</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Return list of Rules objects</returns>
        public static Ruleset FromDirectory(string path, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddDirectory(path, tag);

            return result;
        }

        public static Ruleset FromFile(string filename, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddFile(filename, tag);

            return result;
        }

        public static Ruleset FromString(string jsonstring, string sourcename, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddRaw(jsonstring, sourcename, tag);

            return result;
        }

        public void AddDirectory(string path, string tag)
        {
            if (path == null)
                throw new ArgumentNullException();

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            foreach (string fileName in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                this.AddFile(fileName, tag);
            }
        }

        public void AddFile(string filename, string tag)
        {
            if (filename == null)
                throw new ArgumentNullException();

            if (!File.Exists(filename))
                throw new FileNotFoundException();

            using (StreamReader file = File.OpenText(filename))
            {
                AddRaw(file.ReadToEnd(), filename, tag);
            }
        }

        public void AddRaw(string jsonstring, string sourcename, string tag)
        {
            List<Rule> ruleList = new List<Rule>();
            ruleList = JsonConvert.DeserializeObject<List<Rule>>(jsonstring);
            foreach (Rule r in ruleList)
            {
                r.Source = sourcename;
                r.Tag = tag;

                foreach (SearchPattern p in r.Patterns)
                {
                    if (p.Type == PatternType.Regex_Word || p.Type == PatternType.String)
                    {
                        p.Type = PatternType.Regex;
                        p.Pattern = string.Format(@"\b{0}\b", p.Pattern);
                    }
                }
            }

            // Add only active rules
            foreach (Rule r in ruleList)
            {
                if (r.Active)
                    _rules.Add(r);
            }
        }

        public IEnumerable<Rule> ByLanguage(string language)
        {
            // Otherwise preprare the rules for the content type and store it in cache.
            List<Rule> filteredRules = new List<Rule>();

            foreach (Rule r in _rules)
            {
                if (r.AppliesTo != null && r.AppliesTo.Contains(language))
                {
                    // Put rules with defined contenty type (AppliesTo) on top
                    filteredRules.Insert(0, r);
                }
                else if (r.AppliesTo == null || r.AppliesTo.Length == 0)
                {
                    foreach (SearchPattern p in r.Patterns)
                    {
                        // If applies to is defined and matching put those rules first
                        if (p.AppliesTo != null && p.AppliesTo.Contains(language))
                        {
                            filteredRules.Insert(0, r);
                            continue;
                        }
                        // Generic rules goes to the end of the list
                        if (p.AppliesTo == null)
                        {
                            filteredRules.Add(r);
                            continue;
                        }
                    }
                }
            }

            // Now deal with rule overrides. 
            List<string> idsToRemove = new List<string>();
            foreach (Rule rule in filteredRules)
            {
                if (rule.Overrides != null)
                {
                    foreach (string r in rule.Overrides)
                    {
                        // Mark every rule that is overriden
                        if (!idsToRemove.Contains(r))
                            idsToRemove.Add(r);
                    }
                }
            }

            // Remove marked rules
            foreach (string id in idsToRemove)
            {
                filteredRules.Remove(filteredRules.Find(x => x.Id == id));
            }

            return filteredRules;
        }

        private List<Rule> _rules;
    }
}
