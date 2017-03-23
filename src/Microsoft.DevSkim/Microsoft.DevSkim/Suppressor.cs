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
    public class Suppressor
    {
        public const string KeywordPrefix = "DevSkim:";
        public const string KeywordIgnore = "ignore";        
        public const string KeywordAll = "all";
        public const string KeywordUntil = "until";

        /// <summary>
        /// Creates new instance of Supressor
        /// </summary>
        /// <param name="text">Text to work with</param>        
        public Suppressor(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

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
        /// Parse the line of code to find rule suppressors
        /// </summary>
        private void ParseLine()
        {
            // String.Contains is faster then RegEx. Quickly test if the further parsing is necessary or not
            if (!_text.Contains(KeywordPrefix))
                return;

            string pattern = @"\s*" + KeywordPrefix + @"\s+" + KeywordIgnore + @"\s([^\s]+)(\s+" + KeywordUntil + @"\s\d{4}-\d{2}-\d{2}|)";
            Regex reg = new Regex(pattern);

            Match match = reg.Match(_text);

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

        public virtual string[] GetIssues() 
        {
            return _issues.ToArray();
        }
                
        public DateTime ExpirationDate { get { return _expirationDate; } }
        public int Index { get { return _suppressStart; } }
        public int Length { get { return _suppressLength; } }

        private List<string> _issues = new List<string>();              
        private DateTime _expirationDate = DateTime.MaxValue;
        private string _text = string.Empty;

        private int _suppressStart = -1;
        private int _suppressLength = -1;        
    }
}
