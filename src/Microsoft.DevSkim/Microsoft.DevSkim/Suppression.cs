// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Processor for rule suppressions
    /// </summary>
    public class Suppression
    {
        const string KeywordPrefix = "DevSkim:";
        const string KeywordIgnore = "ignore";        
        const string KeywordAll = "all";
        const string KeywordUntil = "until";

        /// <summary>
        /// Creates new instance of Supressor
        /// </summary>
        /// <param name="text">Text to work with</param>        
        public Suppression(string text)
        {
            if (text == null)
            {
#pragma warning disable IDE0016 // Use 'throw' expression - not supported in < C# 7
                throw new ArgumentNullException("text");
#pragma warning restore IDE0016 // Use 'throw' expression
            }
            _text = text;

            ParseLine();
        }

        /// <summary>
        /// Test if given rule Id is being suppressed
        /// </summary>
        /// <param name="issueId">Rule ID</param>
        /// <returns>True is rule is suppressed</returns>
        public bool IsIssueSuppressed(string issueId)
        {
            bool result = false;
            if (_issues.Contains(KeywordAll) || _issues.Contains(issueId))
                result = true;
             
            return (DateTime.Now < _expirationDate && result);
        }

        /// <summary>
        /// Get list of suppressions string from text
        /// </summary>
        /// <param name="text">Regex matches</param>
        /// <returns></returns>
        public static MatchCollection GetMatches(string text)
        {
            string pattern = @"\s*" + KeywordPrefix + @"\s+" + KeywordIgnore + @"\s([a-zA-Z\d,:]+)(\s+" + KeywordUntil + @"\s\d{4}-\d{2}-\d{2}|)";
            Regex reg = new Regex(pattern);
            return reg.Matches(text);
        }

        /// <summary>
        /// Parse the line of code to find rule suppressors
        /// </summary>
        private void ParseLine()
        {
            // String.Contains is faster then RegEx. Quickly test if the further parsing is necessary or not
            if (!_text.Contains(KeywordPrefix))
                return;

            MatchCollection matches = GetMatches(_text);            

            if (matches.Count > 0 && matches[0].Success)
            {                
                _suppressStart = matches[0].Index;
                _suppressLength = matches[0].Length;
                
                string idString = matches[0].Groups[1].Value.Trim();                

                // Parse date
                if (matches[0].Groups.Count > 2)
                {
                    string date = matches[0].Groups[2].Value;
                    Regex reg = new Regex(@"(\d{4}-\d{2}-\d{2})");
                    Match m = reg.Match(date);
                    if (m.Success)
                    {
                        _expirationDate = DateTime.ParseExact(m.Value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                // parse Ids.                
                if (idString == KeywordAll)
                {
                    _issues.Add(KeywordAll);
                }
                else
                {
                    string[] ids = idString.Split(',');
                    _issues.AddRange(ids);
                }
            }
        }

        /// <summary>
        /// Get issue IDs for the suppression
        /// </summary>
        /// <returns>List of issue IDs</returns>
        public virtual string[] GetIssues() 
        {
            return _issues.ToArray();
        }
        
        /// <summary>
        /// Validity of suppression expresion
        /// </summary>
        /// <returns>True if suppression is in effect</returns>
        public bool IsInEffect {
            get
            {
                bool doesItExists = (Index >= 0 && _issues.Count > 0);
                return (doesItExists && DateTime.Now < _expirationDate);
            }
        }

        /// <summary>
        /// Suppression expiration date
        /// </summary>
        public DateTime ExpirationDate { get { return _expirationDate; } }

        /// <summary>
        /// Suppression expresion start index on the given line
        /// </summary>
        public int Index { get { return _suppressStart; } }

        /// <summary>
        /// Suppression expression length
        /// </summary>
        public int Length { get { return _suppressLength; } }               

        private List<string> _issues = new List<string>();              
        private DateTime _expirationDate = DateTime.MaxValue;
        private string _text = string.Empty;

        private int _suppressStart = -1;
        private int _suppressLength = -1;        
    }
}
