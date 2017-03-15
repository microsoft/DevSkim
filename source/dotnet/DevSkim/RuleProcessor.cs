// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


[assembly: CLSCompliant(true)]
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
            AllowSuppressions = false;
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
        /// Applies given fix on the provided source code line
        /// </summary>
        /// <param name="lineOfCode">Source code line</param>
        /// <param name="fixRecord">Fix record to be applied</param>
        /// <returns>Fixed source code line</returns>
        public static string Fix(string lineOfCode, CodeFix fixRecord)
        {
            string result = string.Empty;

            if (fixRecord.FixType == FixType.RegexSubstitute)
            {
                Regex regex = new Regex(fixRecord.Search);
                result = regex.Replace(lineOfCode, fixRecord.Replace);
            }

            return result;
        }

        /// <summary>
        /// Analyzes given line of code
        /// </summary>
        /// <param name="lineOfCode">Source code</param>
        /// <param name="language">Visual Studio content type</param>
        /// <returns>Array of matches</returns>
        public Match[] Analyze(string lineOfCode, string language)
        {
            // Get rules for the given content type
            IEnumerable<Rule> rules = GetRulesForLanguage(language);            
            List<Match> resultsList = new List<Match>();

            // Go through each rule
            foreach (Rule r in rules)
            {
                Match result = new Match();

                // Skip rules that don't apply based on settings
                if (r.Disabled || (r.Severity == Severity.ManualReview && !AllowManualReview))
                    continue;

                // Go through each matching pattern of the rule
                foreach (SearchPattern p in r.Patterns)
                {
                    // Type == Substring 
                    if (p.PatternType == PatternType.Substring)
                    {
                        result.Location = lineOfCode.ToLower().IndexOf(p.Pattern.ToLower());
                        result.Length = p.Pattern.Length;
                        if (result.Location > -1)
                        {                            
                            result.Rule = r;
                            break; // from pattern loop
                        }
                    }
                    // Type == Regex
                    else if (p.PatternType == PatternType.Regex)
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
                            result.Rule = r;
                            result.Location = m.Index;
                            result.Length = m.Length;
                            break; // from pattern loop                 
                        }
                    }
                }

                // We got matching rule. Let's see if we have a supression on the line
                if (result.Location > -1)
                {
                    Suppressor supp = new Suppressor(lineOfCode, language);
                    // If rule is being suppressed then clear the MatchResult
                    if (supp.IsRuleSuppressed(result.Rule.Id) && AllowSuppressions)
                    {
                        continue;
                    }
                    // Otherwise break out of the loop as we found an issue.
                    // So, no need to scan for more.
                    else
                    {
                        resultsList.Add(result);
                    }
                }
            }

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

        public bool AllowSuppressions { get; set; }

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
