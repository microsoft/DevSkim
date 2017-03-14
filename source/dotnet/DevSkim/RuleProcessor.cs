// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Security.DevSkim
{
    /// <summary>
    /// Heart of DevSkim. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        public RuleProcessor()
        {            
            _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            AllowSuppression = false;
            AllowManualReview = false;
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
        /// Test given source code line for issues
        /// </summary>
        /// <param name="lineOfCode">Source code line</param>
        /// <param name="index">Position in text where to start the scan</param>
        /// <param name="contenttype">Visual Studio content type</param>
        /// <returns>MatchRecord with infomartion of identified issue</returns>
        public Match IsMatch(string lineOfCode, int index, string language)
        {
            Match result = FindMatch(lineOfCode.Substring(index), lineOfCode, language);
            if (result.Location > -1)
                result.Location += index;

            return result;
        }

        /// <summary>
        /// Applies given fix on the provided source code line
        /// </summary>
        /// <param name="lineOfCode">Source code line</param>
        /// <param name="fixRecord">Fix record to be applied</param>
        /// <returns>Fixed source code line</returns>
        public static string Fix(string lineOfCode, CodeFix fixRecord)
        {
            string result = string.Empty;

            if (fixRecord.Type == "regex_substitute")
            {
                Regex regex = new Regex(fixRecord.Search);
                result = regex.Replace(lineOfCode, fixRecord.Replace);
            }

            return result;
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Test given text for issues
        /// </summary>
        /// <param name="lineOfCode">Source code</param>
        /// <param name="language">Visual Studio content type</param>
        /// <returns>MatchRecord with infomartion of identified issue</returns>
        private Match FindMatch(string lineOfCode, string textLine, string language)
        {
            // Get rules for the given content type
            IEnumerable<Rule> rules = GetRulesForLanguage(language);
            Match result = new Match() { Success = false };

            // Go through each rule
            foreach(Rule r in rules)
            {
                // Go through each matching pattern of the rule
                foreach(SearchPattern p in r.Patterns)
                {
                    // Type == Substring 
                    if (p.Type == PatternType.Substring)
                    {
                        result.Location = lineOfCode.ToLower().IndexOf(p.Pattern.ToLower());
                        result.Length = p.Pattern.Length;
                        if (result.Location > -1)
                        {
                            result.Success = true;
                            result.Rule = r;
                            break; // from pattern loop
                        }
                    }
                    // Type == Regex
                    else if (p.Type == PatternType.Regex)
                    {
                        RegexOptions reopt = RegexOptions.None;
                        if (p.Modifiers != null && p.Modifiers.Length > 0)
                        {
                            reopt |= (p.Modifiers.Contains("IGNORECASE")) ? RegexOptions.IgnoreCase : RegexOptions.None;
                            reopt |= (p.Modifiers.Contains("MULTILINE")) ? RegexOptions.Multiline : RegexOptions.None;                            
                        }
                        
                        Regex patRegx = new Regex(p.Pattern, reopt);
                        System.Text.RegularExpressions.Match m = patRegx.Match(lineOfCode);
                        if (m.Success)
                        {
                            result.Success = true;
                            result.Rule = r;
                            result.Location = m.Index;
                            result.Length = m.Length;
                            break; // from pattern loop                 
                        }
                    }                    
                }

                // We got matching rule. Let's see if we have a supression on the line
                if (result.Success && AllowSuppression)
                {
                    Suppressor supp = new Suppressor(textLine, language);
                    // If rule is being suppressed then clear the MatchResult
                    if (supp.IsRuleSuppressed(result.Rule.Id))
                    {
                        result = new Match();
                    }
                    // Otherwise break out of the loop as we found an issue.
                    // So, no need to scan for more.
                    else
                    {                        
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Filters the rules for those matching the content type.
        /// Resolves all the overrides
        /// </summary>
        /// <param name="languages">Languages to filter rules for</param>
        /// <returns>List of rules</returns>
        private IEnumerable<Rule> GetRulesForLanguage(string language)
        {            
            // Do we have the ruleset alrady in cache? If so return it
            if (_rulesCache.ContainsKey(language))
                return _rulesCache[language];

            IEnumerable<Rule> filteredRules = _ruleset.ByLanguage(language); 

            // Add the list to the cache so we save time on the next call
            _rulesCache.Add(language, filteredRules);

            return filteredRules;
        }

        #endregion

        #region Properties

        public Ruleset Rules
        {
            get { return _ruleset; }
            set
            {
                _ruleset = value;
                _rulesCache = new Dictionary<string, IEnumerable<Rule>>();
            }
        }

        public bool AllowSuppression { get; set; }

        public bool AllowManualReview { get; set; }
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
