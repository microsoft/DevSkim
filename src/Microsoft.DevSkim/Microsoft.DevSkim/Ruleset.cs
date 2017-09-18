// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Storage for rules
    /// </summary>
    public class Ruleset : IEnumerable<Rule>
    {
        /// <summary>
        /// Delegate for deserialization error handler
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Error arguments</param>
        public delegate void DeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e);
        
        /// <summary>
        /// Event raised if deserialization error is encoutered while loading JSON rules
        /// </summary>
        public event DeserializationError OnDeserializationError;

        /// <summary>
        /// Creates instance of Ruleset
        /// </summary>
        public Ruleset()
        {
            _rules = new List<Rule>();
        }

        /// <summary>
        /// Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path">Path to rules folder</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Ruleset</returns>
        public static Ruleset FromDirectory(string path, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddDirectory(path, tag);

            return result;
        }

        /// <summary>
        /// Load rules from a file
        /// </summary>
        /// <param name="filename">Filename with rules</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Ruleset</returns>
        public static Ruleset FromFile(string filename, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddFile(filename, tag);

            return result;
        }

        /// <summary>
        /// Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring">JSON string</param>
        /// <param name="sourcename">Name of the source (file, stream, etc..)</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Ruleset</returns>
        public static Ruleset FromString(string jsonstring, string sourcename, string tag)
        {
            Ruleset result = new Ruleset();
            result.AddString(jsonstring, sourcename, tag);

            return result;
        }

        /// <summary>
        /// Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path">Path to rules folder</param>
        /// <param name="tag">Tag for the rules</param>        
        public void AddDirectory(string path, string tag)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                this.AddFile(filename, tag);
            }
        }

        /// <summary>
        /// Load rules from a file
        /// </summary>
        /// <param name="filename">Filename with rules</param>
        /// <param name="tag">Tag for the rules</param>
        public void AddFile(string filename, string tag)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("filename");

            if (!File.Exists(filename))
                throw new FileNotFoundException();

            using (StreamReader file = File.OpenText(filename))
            {
                AddString(file.ReadToEnd(), filename, tag);
            }
        }

        /// <summary>
        /// Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring">JSON string</param>
        /// <param name="sourcename">Name of the source (file, stream, etc..)</param>
        /// <param name="tag">Tag for the rules</param>
        public void AddString(string jsonstring, string sourcename, string tag)
        {
            List<Rule> ruleList = new List<Rule>();
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Error = HandleDeserializationError
            };

            ruleList = JsonConvert.DeserializeObject<List<Rule>>(jsonstring, settings);
            if (ruleList != null)
            {
                foreach (Rule r in ruleList)
                {
                    r.Source = sourcename;
                    r.RuntimeTag = tag;

                    foreach (SearchPattern p in r.Patterns)
                    {
                        if (p.PatternType == PatternType.RegexWord || p.PatternType == PatternType.String)
                        {
                            p.PatternType = PatternType.Regex;
                            p.Pattern = string.Format(@"\b{0}\b", p.Pattern);
                        }
                        else if (p.PatternType == PatternType.Substring)
                        {
                            p.PatternType = PatternType.Regex;
                            p.Pattern = string.Format(@"{0}", p.Pattern);
                        }
                    }

                    if (r.Conditions == null)
                        r.Conditions = new List<SearchCondition>().ToArray();
                }

                _rules.AddRange(ruleList);
            }
        }

        /// <summary>
        /// Add rule into Ruleset
        /// </summary>
        /// <param name="rule"></param>
        public void AddRule(Rule rule)
        {
            _rules.Add(rule);
        }

        /// <summary>
        /// Adds the elements of the collection to the Ruleset
        /// </summary>
        /// <param name="collection">Collection of rules</param>
        public void AddRange(IEnumerable<Rule> collection)
        {
            _rules.AddRange(collection);
        }

        /// <summary>
        /// Filters rules within Ruleset by languages
        /// </summary>
        /// <param name="languages">Languages</param>
        /// <returns>Filtered rules</returns>
        public IEnumerable<Rule> ByLanguages(string[] languages)
        {
            // Otherwise preprare the rules for the content type and store it in cache.
            List<Rule> filteredRules = new List<Rule>();

            foreach (Rule r in _rules)
            {
                if (r.AppliesTo != null && ArrayContains(r.AppliesTo, languages))
                {
                    // Put rules with defined language (applies_to) on top
                    filteredRules.Insert(0, r);
                }
                else if (r.AppliesTo == null || r.AppliesTo.Length == 0)
                {
                    // Put rules without applies_to on the bottom
                    filteredRules.Add(r);
                }
            }

            return filteredRules;
        }

        /// <summary>
        /// Handler for deserialization error
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="errorArgs">Error arguments</param>
        private void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            OnDeserializationError?.Invoke(sender, errorArgs);
        }

        /// <summary>
        /// Tests if array contains given elements
        /// </summary>
        /// <param name="source">Source array</param>
        /// <param name="comps">List of elements to look for</param>
        /// <returns>True if source array contains element from comps array</returns>
        private bool ArrayContains(string[] source, string[] comps)
        {
            foreach (string c in comps)
            {
                if (source.Contains(c))
                    return true;
            }

            return false;
        }

        #region IEnumerable interface

        /// <summary>
        /// Count of rules in the ruleset
        /// </summary>        
        public int Count()
        {
            return _rules.Count();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator GetEnumerator()
        {
            return this._rules.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<Rule> IEnumerable<Rule>.GetEnumerator()
        {
            return this._rules.GetEnumerator();
        }

        #endregion

        #region Fields

        private List<Rule> _rules;
        
        #endregion
    }

}
