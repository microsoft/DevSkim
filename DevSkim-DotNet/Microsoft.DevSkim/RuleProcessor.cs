// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Heart of DevSkim. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        /// <summary>
        ///     Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(RuleSet rules)
        {
            _ruleset = rules;
            _rulesCache = new Dictionary<string, IEnumerable<ConvertedOatRule>>();
            EnableSuppressions = false;
            EnableCache = true;

            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice;

            analyzer = new Analyzer();
            analyzer.SetOperation(new WithinOperation(analyzer));
            analyzer.SetOperation(new ScopedRegexOperation(analyzer));
        }

        /// <summary>
        ///     Enables caching of rules queries. Increases performance and memory use!
        /// </summary>
        public bool EnableCache { get; set; }

        /// <summary>
        ///     Enable suppresion syntax checking during analysis
        /// </summary>
        public bool EnableSuppressions { get; set; }

        /// <summary>
        ///     Ruleset to be used for analysis
        /// </summary>
        public RuleSet Rules
        {
            get { return _ruleset; }
            set
            {
                _ruleset = value;
                _rulesCache = new Dictionary<string, IEnumerable<ConvertedOatRule>>();
            }
        }

        /// <summary>
        ///     Sets severity levels for analysis
        /// </summary>
        public Severity SeverityLevel { get; set; }

        private Analyzer analyzer;

        /// <summary>
        ///     Applies given fix on the provided source code line
        /// </summary>
        /// <param name="text"> Source code line </param>
        /// <param name="fixRecord"> Fix record to be applied </param>
        /// <returns> Fixed source code line </returns>
        public static string Fix(string text, CodeFix fixRecord)
        {
            string result = string.Empty;

            if (fixRecord.FixType == FixType.RegexReplace)
            {
                if (fixRecord.Pattern is { })
                {
                    //TODO: Better pattern search and modifiers
                    Regex regex = new Regex(fixRecord.Pattern.Pattern);
                    result = regex.Replace(text, fixRecord.Replacement);
                }
            }

            return result;
        }

        /// <summary>
        ///     Analyzes given line of code
        /// </summary>
        /// <param name="text"> Source code </param>
        /// <param name="language"> Language </param>
        /// <returns> Array of matches </returns>
        public Issue[] Analyze(string text, string language)
        {
            return Analyze(text, new string[] { language });
        }

        public Issue[] Analyze(string text, int lineNumber, string language)
        {
            return Analyze(text, new string[] { language }, lineNumber);
        }


        /// <summary>
        ///     Analyzes given line of code
        /// </summary>
        /// <param name="text"> Source code </param>
        /// <param name="languages"> List of languages </param>
        /// <returns> Array of matches </returns>
        public Issue[] Analyze(string text, string[] languages, int lineNumber = -1)
        {
            // Get rules for the given content type
            IEnumerable<CST.OAT.Rule> rules = GetRulesForLanguages(languages).Where(x => !x.DevSkimRule.Disabled && SeverityLevel.HasFlag(x.DevSkimRule.Severity));
            // Skip rules that are disabled or don't have the right severity
            //    if (rule.Disabled || !SeverityLevel.HasFlag(rule.Severity))
            //        continue;

            var filtered = rules.Where(x => x.Name == "DS126186");

            List<Issue> resultsList = new List<Issue>();
            TextContainer textContainer = new TextContainer(text, (languages.Length > 0) ? languages[0] : string.Empty, lineNumber);

            foreach(var capture in analyzer.GetCaptures(rules, textContainer))
            {
                // Turn matches into boundaries.
                var matches = capture.Captures;
                foreach (var cap in capture.Captures)
                {
                    if (cap is TypedClauseCapture<List<Boundary>> tcc)
                    {
                        if (capture.Rule is ConvertedOatRule orh)
                        {
                            foreach (var boundary in tcc.Result)
                            {
                                var issue = new Issue(Boundary: boundary, StartLocation: textContainer.GetLocation(boundary.Index), EndLocation: textContainer.GetLocation(boundary.Index + boundary.Length), Rule: orh.DevSkimRule);
                                if (EnableSuppressions)
                                {
                                    var supp = new Suppression(textContainer, (lineNumber > 0) ? lineNumber : issue.StartLocation.Line);
                                    var supissue = supp.GetSuppressedIssue(issue.Rule.Id);
                                    if (supissue is null)
                                    {
                                        resultsList.Add(issue);
                                    }
                                    //Otherwise add the suppression info instead
                                    else
                                    {
                                        issue.IsSuppressionInfo = true;

                                        if (!resultsList.Any(x => x.Rule.Id == issue.Rule.Id && x.Boundary.Index == issue.Boundary.Index))
                                            resultsList.Add(issue);
                                    }
                                }
                                else
                                {
                                    resultsList.Add(issue);
                                }
                            }

                        }
                    }
                }
            }

            // Deal with overrides
            List<Issue> removes = new List<Issue>();
            foreach (Issue m in resultsList)
            {
                if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                {
                    foreach (string ovrd in m.Rule.Overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list
                        foreach (Issue om in resultsList.FindAll(x => x.Rule.Id == ovrd))
                        {
                            if (om.Boundary.Index >= m.Boundary.Index &&
                                om.Boundary.Index <= m.Boundary.Index + m.Boundary.Length)
                                removes.Add(om);
                        }
                    }
                }
            }

            // Remove overriden rules
            resultsList.RemoveAll(x => removes.Contains(x));

            return resultsList.ToArray();
        }

        private IEnumerable<ConvertedOatRule> GetOatRulesForLanguages(string[] languages)
        {
            return GetRulesForLanguages(languages);
        }

        /// <summary>
        ///     Cache for rules filtered by content type
        /// </summary>
        private Dictionary<string, IEnumerable<ConvertedOatRule>> _rulesCache;

        private RuleSet _ruleset;

        /// <summary>
        ///     Filters the rules for those matching the content type. Resolves all the overrides
        /// </summary>
        /// <param name="languages"> Languages to filter rules for </param>
        /// <returns> List of rules </returns>
        private IEnumerable<ConvertedOatRule> GetRulesForLanguages(string[] languages)
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

            IEnumerable<ConvertedOatRule> filteredRules = _ruleset.ByLanguages(languages);

            // Add the list to the cache so we save time on the next call
            if (EnableCache && filteredRules.Any())
            {
                _rulesCache.Add(langid, filteredRules);
            }

            return filteredRules;
        }
    }
}