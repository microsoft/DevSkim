// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Security.DevSkim
{
    /// <summary>
    /// Processor for rule suppressions
    /// </summary>
    public class Suppressor
    {
        const string SUPPRESS_RULE_PREFFIX = "DevSkim:";
        const string SUPPRESS_RULE_IGNORE = "ignore";
        const string SUPPRESS_RULE_ALL = "all";
        const string SUPPRESS_RULE_UNTIL = "until";

        /// <summary>
        /// Creates instance of Supressor
        /// </summary>
        /// <param name="lineOfCode">Line of code to work with</param>
        /// <param name="language">Visual Studio content yype</param>
        public Suppressor(string lineOfCode, string language)
        {
            if (lineOfCode == null || language == null)
                throw new ArgumentNullException();

            _text = lineOfCode;
            _language = language;

            ParseLine();
        }

        /// <summary>
        /// Test if given rule Id is being suppressed
        /// </summary>
        /// <param name="ruleId">Rule ID</param>
        /// <returns>True is rule is suppressed</returns>
        public bool IsRuleSuppressed(string ruleId)
        {
            bool result = false;
            if (_rulesAll || _rules.Contains(ruleId))
                result = true;
            
            if (_date > DateTime.MinValue)
                return (DateTime.Now < _date && result);
            else
                return result;
        }

        /// <summary>
        /// Supress all rules
        /// </summary>
        /// <returns>Line of code with suppression set</returns>
        public string SuppressAll()
        {
            return SuppressAll(DateTime.MinValue);
        }

        /// <summary>
        /// Supress all rules
        /// </summary>
        /// <param name="date">Date of suppression expiration (DateTime.MinValue for no expiration)</param>
        /// <returns>Line of code with suppression set</returns>
        public string SuppressAll(DateTime date)
        {
            return SuppressRule(null, date);
        }

        /// <summary>
        /// Suppress given rule
        /// </summary>
        /// <param name="ruleId">Rule Id to suppress (null for all)</param>        
        /// <returns>Line of code with suppression set</returns>
        public string SuppressRule(string ruleId)
        {
            return SuppressRule(ruleId, DateTime.MinValue);
        }

        /// <summary>
        /// Suppress given rule
        /// </summary>
        /// <param name="ruleId">Rule Id to suppress (null for all)</param>
        /// <param name="date">Date of suppression expiration (DateTime.MinValue for no expiration)</param>
        /// <returns>Line of code with suppression set</returns>
        public string SuppressRule(string ruleId, DateTime date)
        {
            // Get `rules list` or `all` keyword
            string ruleList = string.Empty;
            if (string.IsNullOrEmpty(ruleId))
            {
                ruleList = SUPPRESS_RULE_ALL;
            }
            // Create the rules list
            else
            {                
                if (!_rules.Contains(ruleId))
                    _rules.Add(ruleId);

                foreach(string id in _rules)
                {
                    ruleList = string.Concat(ruleList, id, ",");
                }

                ruleList = ruleList.Remove(ruleList.Length - 1);
            }
            
            // Prepare basic ignore command
            string command = string.Format("{0} {1} {2}", SUPPRESS_RULE_PREFFIX, SUPPRESS_RULE_IGNORE, ruleList);
            string expiration = string.Empty;

            // Prepare expiration date if it is set
            if (_date > DateTime.MinValue)
            {
                expiration = string.Format(" {0} {1}", SUPPRESS_RULE_UNTIL, _date.ToString("yyyy-MM-dd"));
            }

            if (date > DateTime.MinValue && _date < date)
            {                
                expiration = string.Format(" {0} {1}", SUPPRESS_RULE_UNTIL, date.ToString("yyyy-MM-dd"));
            }

            command = string.Concat(command, expiration);

            // If we are dealing with existing suppress we are going to refresh it
            // othewrise add completely new comment with suppressor
            string result = _text;
            if (_suppressStart >= 0)
            {
                result = result.Remove(_suppressStart, _suppressLength);
                result = result.Insert(_suppressStart, command);
            }
            else
            {
                result = string.Concat(result, " ", Language.Comment(command, _language));
            }

            return result;
        }

        /// <summary>
        /// Parse the line of code to find rule suppressors
        /// </summary>
        private void ParseLine()
        {
            // String.Contains is faster then RegEx. Quickly test if the further parsing is necessary or not
            if (!_text.Contains(SUPPRESS_RULE_PREFFIX))
                return;

            string pattern = @"\s*" + SUPPRESS_RULE_PREFFIX + @"\s+" + SUPPRESS_RULE_IGNORE + @"\s([^\s]+)(\s+" + SUPPRESS_RULE_UNTIL + @"\s\d{4}-\d{2}-\d{2}|)";
            Regex reg = new Regex(pattern);

            System.Text.RegularExpressions.Match match = reg.Match(_text);

            if (match.Success)
            {
                _suppressStart = match.Index;
                _suppressLength = match.Length;
                                
                string idString = match.Groups[1].Value.Trim();

                // Parse date
                if (match.Groups.Count > 2)
                {
                    string date = match.Groups[2].Value;
                    reg = new Regex(@"(\d{4}-\d{2}-\d{2})");
                    System.Text.RegularExpressions.Match m = reg.Match(date);
                    if (m.Success)
                    {
                        _date = DateTime.ParseExact(m.Value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                // parse Ids.                
                if (idString == SUPPRESS_RULE_ALL)
                {
                    _rulesAll = true;
                }
                else
                {
                    string[] ids = idString.Split(',');
                    _rules.AddRange(ids);
                }
            }
        }

        private List<string> _rules = new List<string>();
        private bool _rulesAll = false;
        private DateTime _date = DateTime.MinValue;
        private string _text = string.Empty;

        private int _suppressStart = -1;
        private int _suppressLength = -1;
        private string _language;
    }
}
