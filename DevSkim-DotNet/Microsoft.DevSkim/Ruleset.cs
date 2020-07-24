// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Storage for rules
    /// </summary>
    public class RuleSet : IEnumerable<Rule>
    {
        /// <summary>
        ///     Creates instance of Ruleset
        /// </summary>
        public RuleSet()
        {
            _rules = new List<Rule>();
            _oatRules = new List<ConvertedOatRule>();
        }

        internal ConvertedOatRule DevSkimRuleToConvertedOatRule(Rule rule)
        {
            return new ConvertedOatRule(rule.Id, rule)
            {
                // The specifics for the actual OAT Rule.
            };
        }

        /// <summary>
        ///     Delegate for deserialization error handler
        /// </summary>
        /// <param name="sender"> Sender object </param>
        /// <param name="e"> Error arguments </param>
        public delegate void DeserializationError(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs e);

        /// <summary>
        ///     Event raised if deserialization error is encoutered while loading JSON rules
        /// </summary>
        public event DeserializationError? OnDeserializationError;

        /// <summary>
        ///     Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path"> Path to rules folder </param>
        /// <param name="tag"> Tag for the rules </param>
        /// <returns> Ruleset </returns>
        public static RuleSet FromDirectory(string path, string? tag = null)
        {
            RuleSet result = new RuleSet();
            result.AddDirectory(path, tag);

            return result;
        }

        /// <summary>
        ///     Load rules from a file
        /// </summary>
        /// <param name="filename"> Filename with rules </param>
        /// <param name="tag"> Tag for the rules </param>
        /// <returns> Ruleset </returns>
        public static RuleSet FromFile(string filename, string? tag = null)
        {
            RuleSet result = new RuleSet();
            result.AddFile(filename, tag);

            return result;
        }

        /// <summary>
        ///     Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring"> JSON string </param>
        /// <param name="sourcename"> Name of the source (file, stream, etc..) </param>
        /// <param name="tag"> Tag for the rules </param>
        /// <returns> Ruleset </returns>
        public static RuleSet FromString(string jsonstring, string sourcename = "string", string? tag = null)
        {
            RuleSet result = new RuleSet();
            result.AddString(jsonstring, sourcename, tag);

            return result;
        }

        /// <summary>
        ///     Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path"> Path to rules folder </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddDirectory(string path, string? tag = null)
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
        ///     Load rules from a file
        /// </summary>
        /// <param name="filename"> Filename with rules </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddFile(string filename, string? tag = null)
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
        ///     Adds the elements of the collection to the Ruleset
        /// </summary>
        /// <param name="collection"> Collection of rules </param>
        public void AddRange(IEnumerable<Rule> collection)
        {
            _rules.AddRange(collection);
            _oatRules.AddRange(collection.Select(x => DevSkimRuleToConvertedOatRule(x)));
        }

        /// <summary>
        ///     Add rule into Ruleset
        /// </summary>
        /// <param name="rule"> </param>
        public void AddRule(Rule rule)
        {
            _rules.Add(rule);
            _oatRules.Add(DevSkimRuleToConvertedOatRule(rule));
        }

        /// <summary>
        ///     Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring"> JSON string </param>
        /// <param name="sourcename"> Name of the source (file, stream, etc..) </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddString(string jsonstring, string sourcename, string? tag = null)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Error = HandleDeserializationError
            };

            List<Rule>? ruleList = JsonConvert.DeserializeObject<List<Rule>>(jsonstring, settings);
            if (ruleList is List<Rule>)
            {
                foreach (Rule r in ruleList)
                {
                    r.Source = sourcename;
                    r.RuntimeTag = tag;

                    if (r.Patterns == null)
                        r.Patterns = new SearchPattern[] { };

                    foreach (SearchPattern pattern in r.Patterns)
                    {
                        SanitizePatternRegex(pattern);
                    }

                    if (r.Conditions == null)
                        r.Conditions = new SearchCondition[] { };

                    foreach (SearchCondition condition in r.Conditions)
                    {
                        if (condition.Pattern is { })
                        {
                            SanitizePatternRegex(condition.Pattern);
                        }
                    }
                }

                _rules.AddRange(ruleList);
                _oatRules.AddRange(ruleList.Select(x => DevSkimRuleToConvertedOatRule(x)));
            }
        }

        /// <summary>
        ///     Filters rules within Ruleset by languages
        /// </summary>
        /// <param name="languages"> Languages </param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByLanguages(string[] languages)
        {
            return _oatRules.Where(x => x.Rule.AppliesTo is string[] appliesList && ArrayContains(appliesList,languages));
        }

        /// <summary>
        ///     Count of rules in the ruleset
        /// </summary>
        public int Count()
        {
            return _rules.Count;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns> Enumerator </returns>
        public IEnumerator GetEnumerator()
        {
            return this._rules.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns> Enumerator </returns>
        IEnumerator<Rule> IEnumerable<Rule>.GetEnumerator()
        {
            return this._rules.GetEnumerator();
        }

        private List<Rule> _rules;
        private List<ConvertedOatRule> _oatRules;

        /// <summary>
        ///     Tests if array contains given elements
        /// </summary>
        /// <param name="source"> Source array </param>
        /// <param name="comps"> List of elements to look for </param>
        /// <returns> True if source array contains element from comps array </returns>
        private bool ArrayContains(string[] source, string[] comps)
        {
            foreach (string c in comps)
            {
                if (source.Contains(c))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Handler for deserialization error
        /// </summary>
        /// <param name="sender"> Sender object </param>
        /// <param name="errorArgs"> Error arguments </param>
        private void HandleDeserializationError(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            OnDeserializationError?.Invoke(sender, errorArgs);
        }

        /// <summary>
        ///     Method santizes pattern to be a valid regex
        /// </summary>
        /// <param name="pattern"> </param>
        private void SanitizePatternRegex(SearchPattern pattern)
        {
            if (pattern.PatternType == PatternType.RegexWord)
            {
                pattern.PatternType = PatternType.Regex;
                pattern.Pattern = string.Format(@"\b{0}\b", pattern.Pattern);
            }
            else if (pattern.PatternType == PatternType.String)
            {
                pattern.PatternType = PatternType.Regex;
                pattern.Pattern = string.Format(@"\b{0}\b", Regex.Escape(pattern.Pattern));
            }
            else if (pattern.PatternType == PatternType.Substring)
            {
                pattern.PatternType = PatternType.Regex;
                pattern.Pattern = string.Format(@"{0}", Regex.Escape(pattern.Pattern));
            }
        }
    }
}