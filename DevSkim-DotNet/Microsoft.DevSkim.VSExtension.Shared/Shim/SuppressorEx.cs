// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim.VSExtension
{
    public class SuppressionEx : Suppression
    {
        public SuppressionEx(DevSkimError error, string language)
            : base(text: new TextContainer(error.Snapshot.GetText(), error.Snapshot.ContentType.TypeName), lineNumber: error.LineNumber + 1)
        {
            _error = error;
            _language = language;
            ParseOtherLineSuppressions();
        }

        private void ParseOtherLineSuppressions()
        {
            var settings = Settings.GetSettings();
            // If we have multiple lines to look at
            if (_text != null && settings.UsePreviousLineSuppression)
            {
                // If the line with the issue doesn't contain a suppression check the lines above it
                if (!_lineText.Contains(KeywordPrefix))
                {
                    if (_lineNumber > 1)
                    {
                        var content = _text.GetLineContent(--_lineNumber);
                        if (content.Contains(Language.GetCommentSuffix(_text.Language)))
                        {
                            while (_lineNumber >= 1)
                            {
                                if (reg.IsMatch(_text.GetLineContent(_lineNumber)))
                                {
                                    _lineText = _text.GetLineContent(_lineNumber);
                                    break;
                                }
                                else if (_text.GetLineContent(_lineNumber).Contains(Language.GetCommentPrefix(_text.Language)))
                                {
                                    break;
                                }
                                _lineNumber--;
                            }
                        }
                        else if (content.Contains(Language.GetCommentInline(_text.Language)))
                        {
                            _lineText = content;
                        }
                    }
                }
                Match match = reg.Match(_lineText);

                if (match.Success)
                {
                    _suppressStart = match.Index;
                    _suppressLength = match.Length;

                    string idString = match.Groups[1].Value.Trim();
                    IssuesListIndex = match.Groups[1].Index;

                    // Parse date
                    if (match.Groups.Count > 2)
                    {
                        string date = match.Groups[2].Value;
                        reg = new Regex(@"(\d{4}-\d{2}-\d{2})");
                        Match m = reg.Match(date);
                        if (m.Success)
                        {
                            try
                            {
                                _expirationDate = DateTime.ParseExact(m.Value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch (FormatException)
                            {
                                _expirationDate = DateTime.MinValue;
                            }
                        }
                    }

                    // parse Ids.
                    if (idString == KeywordAll)
                    {
                        _issues.Add(new SuppressedIssue()
                        {
                            ID = KeywordAll,
                            Boundary = new Boundary()
                            {
                                Index = IssuesListIndex,
                                Length = KeywordAll.Length
                            }
                        });
                    }
                    else
                    {
                        string[] ids = idString.Split(',');
                        int index = IssuesListIndex;
                        foreach (string id in ids)
                        {
                            if (!_issues.Any(x => x.ID == id))
                            {
                                _issues.Add(new SuppressedIssue()
                                {
                                    ID = id,
                                    Boundary = new Boundary()
                                    {
                                        Index = index,
                                        Length = id.Length
                                    }
                                });
                            }
                            index += id.Length + 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Supress all rules
        /// </summary>
        /// <returns> Line of code with suppression set </returns>
        public string SuppressAll()
        {
            return SuppressAll(DateTime.MaxValue);
        }

        //public SuppressionEx(TextContainer)
        /// <summary>
        ///     Supress all rules
        /// </summary>
        /// <param name="date"> Date of suppression expiration (DateTime.MaxValue for no expiration) </param>
        /// <returns> Line of code with suppression set </returns>
        public string SuppressAll(DateTime date)
        {
            return SuppressIssue(null, date);
        }

        /// <summary>
        ///     Suppress given rule
        /// </summary>
        /// <param name="issueId"> Rule Id to suppress (null for all) </param>
        /// <returns> Line of code with suppression set </returns>
        public string SuppressIssue(string issueId)
        {
            return SuppressIssue(issueId, DateTime.MaxValue);
        }

        /// <summary>
        ///     Suppress given rule
        /// </summary>
        /// <param name="issueId"> Rule Id to suppress (null for all) </param>
        /// <param name="date"> Date of suppression expiration (DateTime.MaxValue for no expiration) </param>
        /// <returns> Line of code with suppression set </returns>
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
                // Keep suppressions for any existing issues
                var ids = _issues.Select(x => x.ID).ToHashSet();
                ids.Add(issueId);                    

                ruleList = string.Join(",", ids);
            }

            // Prepare basic ignore command
            string command = string.Format("{0} {1} {2}", KeywordPrefix, KeywordIgnore, ruleList);
            string expiration = string.Empty;

            // Prepare expiration date if it is set
            if (date != DateTime.MaxValue)
            {
                expiration = string.Format(" {0} {1}", KeywordUntil, date.ToString("yyyy-MM-dd"));
            }

            command = string.Concat(command, expiration);

            // If we are dealing with existing suppress we are going to refresh it othewrise add completely
            // new comment with suppressor
            string result = _error.LineText;
            if (this.Index > 0 && this.GetSuppressedIssue(issueId) != null)
            {
                result = UpdateSuppression(command);
            }
            else
            {
                Settings set = Settings.GetSettings();
                if (set.UseBlockSuppression)
                {
                    if (Language.GetCommentPrefix(_language) != null)
                    {
                        command = string.Format("{0}{1}{2}",
                            Language.GetCommentPrefix(_language),
                            command,
                            Language.GetCommentSuffix(_language));
                    }
                    else
                    {
                        command = string.Format("{0}{1}",
                            Language.GetCommentInline(_language),
                            command);
                    }
                }
                else
                {
                    if (Language.GetCommentInline(_language) != null)
                    {
                        command = string.Format("{0}{1}",
                            Language.GetCommentInline(_language),
                            command);
                    }
                    else
                    {
                        command = string.Format("{0}{1}{2}",
                            Language.GetCommentPrefix(_language),
                            command,
                            Language.GetCommentSuffix(_language));
                    }
                }

                if (set.UsePreviousLineSuppression)
                {
                    var reg = new Regex("^([\\s]*)");
                    if (reg.IsMatch(result))
                    {
                        result = string.Format("{0}{1}{2}{3}", reg.Match(result), command, Environment.NewLine, result);
                    }
                    else
                    {
                        result = string.Format("{0}{1}{2}", command, Environment.NewLine, result);
                    }
                }
                else
                {
                    result = string.Format("{0} {1}", result, command);
                }
            }

            return result;
        }

        public string UpdateSuppression(string command)
        {
            var full = _error.LineAndSuppressionCommentTrackingSpan.GetText(_error.Snapshot);

            var reg = new Regex(Suppression.pattern);

            return reg.Replace(full, command);
        }

        private DevSkimError _error;
        private string _language;
    }
}