// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


[assembly: CLSCompliant(true)]
namespace Microsoft.DevSkim
{
    /// <summary>
    /// Heart of DevSkim. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor()
        {            
            _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            EnableSuppressions = false;
            EnableCache = true;

            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice;
        }

        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(Ruleset rules) : this()
        {
            this.Rules = rules;
        }       

        #region Public Methods

        /// <summary>
        /// Applies given fix on the provided source code line
        /// </summary>
        /// <param name="text">Source code line</param>
        /// <param name="fixRecord">Fix record to be applied</param>
        /// <returns>Fixed source code line</returns>
        public static string Fix(string text, CodeFix fixRecord)
        {
            string result = string.Empty;

            if (fixRecord.FixType == FixType.RegexReplace)
            {
                //TODO: Better pattern search and modifiers
                Regex regex = new Regex(fixRecord.Pattern.Pattern);
                result = regex.Replace(text, fixRecord.Replacement);
            }

            return result;
        }

        /// <summary>
        /// Analyzes given line of code
        /// </summary>
        /// <param name="text">Source code</param>
        /// <param name="language">Language</param>
        /// <returns>Array of matches</returns>
        public Issue[] Analyze(string text, string language)
        {
            return Analyze(text, new string[] { language });
        }

        /// <summary>
        /// Analyzes given line of code
        /// </summary>
        /// <param name="text">Source code</param>
        /// <param name="languages">List of languages</param>
        /// <returns>Array of matches</returns>
        public Issue[] Analyze(string text, string[] languages)
        {
            // Get rules for the given content type
            IEnumerable<Rule> rules = GetRulesForLanguages(languages);
            List<Issue> resultsList = new List<Issue>();

            // Go through each rule
            foreach (Rule r in rules)
            {
                List<Issue> matchList = new List<Issue>();

                // Skip rules that don't apply based on settings
                if (r.Disabled || !SeverityLevel.HasFlag(r.Severity))
                    continue;

                // Go through each matching pattern of the rule
                foreach (SearchPattern p in r.Patterns)
                {
                    RegexOptions reopt = RegexOptions.None;
                    if (p.Modifiers != null && p.Modifiers.Length > 0)
                    {
                        reopt |= (p.Modifiers.Contains("i")) ? RegexOptions.IgnoreCase : RegexOptions.None;
                        reopt |= (p.Modifiers.Contains("m")) ? RegexOptions.Multiline : RegexOptions.None;
                    }

                    Regex patRegx = new Regex(p.Pattern, reopt);
                    MatchCollection matches = patRegx.Matches(text);
                    if (matches.Count > 0)
                    {
                        foreach (System.Text.RegularExpressions.Match m in matches)
                        {
                            matchList.Add(new Issue() { Index = m.Index, Length = m.Length, Rule = r });
                        }
                        break; // from pattern loop                 
                    }                    
                }

                // We got matching rule and suppression are enabled,
                // let's see if we have a supression on the line
                if (EnableSuppressions && matchList.Count > 0)
                {
                    Suppression supp = new Suppression(text);
                    foreach (Issue result in matchList)
                    {
                        // If rule is NOT being suppressed then useit
                        if (!supp.IsIssueSuppressed(result.Rule.Id))
                        {
                            resultsList.Add(result);
                        }
                    }
                }
                // Otherwise put matchlist to resultlist 
                else
                {
                    resultsList.AddRange(matchList);
                }
            }
            
            // Deal with overrides 
            List<Issue> removes = new List<Issue>();
            foreach (Issue m in resultsList)
            {
                if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                {
                    foreach(string ovrd in m.Rule.Overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list   
                        foreach(Issue om in resultsList.FindAll(x => x.Rule.Id == ovrd))
                        {
                            if (m.Index == om.Index)
                                removes.Add(om);
                        }
                    }
                }
            }

            // Remove overriden rules
            resultsList.RemoveAll(x => removes.Contains(x));

            return resultsList.ToArray();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Filters the rules for those matching the content type.
        /// Resolves all the overrides
        /// </summary>
        /// <param name="languages">Languages to filter rules for</param>
        /// <returns>List of rules</returns>
        private IEnumerable<Rule> GetRulesForLanguages(string[] languages)
        {            
            string langid = string.Empty;

            if (EnableCache)
            {
                Array.Sort(languages);
                // Make language id for cache purposes                
                langid = string.Join(":", languages);
                // Do we have the ruleset alrady in cache? If so return it
                if (_rulesCache.ContainsKey(langid))
                    return _rulesCache[langid];
            }
            
            IEnumerable<Rule> filteredRules = _ruleset.ByLanguages(languages);

            // Add the list to the cache so we save time on the next call
            if (EnableCache && filteredRules.Count() > 0)
            {             
                _rulesCache.Add(langid, filteredRules);
            }

            return filteredRules;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Ruleset to be used for analysis
        /// </summary>
        public Ruleset Rules
        {
            get { return _ruleset; }
            set
            {
                _ruleset = value;
                _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            }
        }

        /// <summary>
        /// Sets severity levels for analysis
        /// </summary>
        public Severity SeverityLevel { get; set; }

        /// <summary>
        /// Enable suppresion syntax checking during analysis
        /// </summary>
        public bool EnableSuppressions { get; set; }

        /// <summary>
        /// Enables caching of rules queries.
        /// Increases performance and memory use!
        /// </summary>
        public bool EnableCache { get; set; }
        #endregion

        #region Fields 

        private Ruleset _ruleset;

        /// <summary>
        /// Cache for rules filtered by content type
        /// </summary>
        private Dictionary<string, IEnumerable<Rule>> _rulesCache;
        #endregion
    }
}
