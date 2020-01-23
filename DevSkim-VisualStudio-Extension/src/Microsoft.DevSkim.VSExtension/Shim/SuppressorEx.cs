using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.DevSkim.VSExtension
{
    public class SuppressionEx : Suppression
    {
        const string KeywordPrefix = "DevSkim:";
        const string KeywordIgnore = "ignore";
        const string KeywordAll = "all";
        const string KeywordUntil = "until";

        public SuppressionEx(string text, string language) 
            : base(text)
        {            
            _text = text;
            _language = language;
            foreach (SuppressedIssue issue in base.GetIssues())
            {
                _issues.Add(issue.ID);
            }
            //ParseLine();
        }

        /// <summary>
        /// Supress all rules
        /// </summary>
        /// <returns>Line of code with suppression set</returns>
        public string SuppressAll()
        {
            return SuppressAll(DateTime.MaxValue);
        }

        /// <summary>
        /// Supress all rules
        /// </summary>
        /// <param name="date">Date of suppression expiration (DateTime.MaxValue for no expiration)</param>
        /// <returns>Line of code with suppression set</returns>
        public string SuppressAll(DateTime date)
        {
            return SuppressIssue(null, date);
        }

        /// <summary>
        /// Suppress given rule
        /// </summary>
        /// <param name="issueId">Rule Id to suppress (null for all)</param>        
        /// <returns>Line of code with suppression set</returns>
        public string SuppressIssue(string issueId)
        {
            return SuppressIssue(issueId, DateTime.MaxValue);
        }

        /// <summary>
        /// Suppress given rule
        /// </summary>
        /// <param name="issueId">Rule Id to suppress (null for all)</param>
        /// <param name="date">Date of suppression expiration (DateTime.MaxValue for no expiration)</param>
        /// <returns>Line of code with suppression set</returns>
        public string SuppressIssue(string issueId, DateTime date)
        {
            // Get `rules list` or `all` keyword
            string ruleList = string.Empty;            
            if (string.IsNullOrEmpty(issueId))
            {
                ruleList = KeywordAll;
            }
            // Create the rules list
            else
            {
                if (!_issues.Contains(issueId))
                    _issues.Add(issueId);

                ruleList = string.Join(",", _issues.ToArray());
            }

            // Prepare basic ignore command
            string command = string.Format("{0} {1} {2}", KeywordPrefix, KeywordIgnore, ruleList);
            string expiration = string.Empty;

            // Prepare expiration date if it is set
            if (ExpirationDate != DateTime.MaxValue)
            {
                expiration = string.Format(" {0} {1}", KeywordUntil, ExpirationDate.ToString("yyyy-MM-dd"));
            }

            // Set expiration to the closer date
            if (date < DateTime.MaxValue && date < ExpirationDate)
            {
                expiration = string.Format(" {0} {1}", KeywordUntil, date.ToString("yyyy-MM-dd"));
            }

            command = string.Concat(command, expiration);

            // If we are dealing with existing suppress we are going to refresh it
            // othewrise add completely new comment with suppressor
            string result = _text;
            if (this.Index > 0)
            {
                result = result.Remove(Index, Length);
                result = result.Insert(Index, command);
            }
            else
            {
                result = string.Format("{0} {1}{2}{3}", 
                                        result, 
                                        Language.GetCommentPrefix(_language),
                                        command, 
                                        Language.GetCommentSuffix(_language)); 
            }

            return result;
        }
   
        private List<string> _issues = new List<string>();
        private string _language;
        private string _text;
    }
}
