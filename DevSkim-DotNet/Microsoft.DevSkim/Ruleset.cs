﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Storage for rules
    /// </summary>
    public class RuleSet : IEnumerable<ConvertedOatRule>
    {
        /// <summary>
        ///     Creates instance of Ruleset
        /// </summary>
        public RuleSet()
        {
            _oatRules = new List<ConvertedOatRule>();
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
        public event DeserializationError? OnDeserializationErrorEventHandler;

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
                throw new ArgumentNullException(nameof(path));

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
                throw new ArgumentException(nameof(filename));

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
            foreach (var rule in collection.Select(DevSkimRuleToConvertedOatRule))
            {
                if (rule != null)
                    _oatRules.Add(rule);
            }
        }

        /// <summary>
        ///     Add rule into Ruleset
        /// </summary>
        /// <param name="rule"> </param>
        public void AddRule(Rule rule)
        {
            if (DevSkimRuleToConvertedOatRule(rule) is ConvertedOatRule cor)
                _oatRules.Add(cor);
        }

        /// <summary>
        ///     Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring"> JSON string </param>
        /// <param name="sourcename"> Name of the source (file, stream, etc..) </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddString(string jsonstring, string sourcename, string? tag = null)
        {
            AddRange(StringToRules(jsonstring, sourcename, tag));
        }

        /// <summary>
        ///     Filters rules within Ruleset by languages
        /// </summary>
        /// <param name="languages"> Languages </param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByLanguages(string[] languages)
        {
            var rulesOut = _oatRules.Where(x => x != null && (x.DevSkimRule.AppliesTo.Count == 0 || x.DevSkimRule.AppliesTo.Any(y => languages.Contains(y))));
            rulesOut = rulesOut.Where(x => x != null && !x.DevSkimRule.DoesNotApplyTo.Any(x => languages.Contains(x)));
            return rulesOut;
        }

        /// <summary>
        ///     Count of rules in the ruleset
        /// </summary>
        public int Count()
        {
            return _oatRules.Count;
        }

        public ConvertedOatRule? DevSkimRuleToConvertedOatRule(Rule rule)
        {
            if (rule == null)
                return null;
            var clauses = new List<Clause>();
            int clauseNumber = 0;
            var expression = new StringBuilder("(");
            foreach (var pattern in rule.Patterns)
            {
                if (pattern.Pattern != null)
                {
                    var scopes = pattern.Scopes ?? new PatternScope[] { PatternScope.All };
                    var modifiers = pattern.Modifiers ?? Array.Empty<string>();
                    if (clauses.Where(x => x is ScopedRegexClause src &&
                        src.Arguments.SequenceEqual(modifiers) && src.Scopes.SequenceEqual(scopes)) is IEnumerable<Clause> filteredClauses &&
                        filteredClauses.Any() && filteredClauses.First().Data is List<string> found)
                    {
                        found.Add(pattern.Pattern);
                    }
                    else
                    {
                        clauses.Add(new ScopedRegexClause(scopes)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Data = new List<string>() { pattern.Pattern },
                            Capture = true,
                            Arguments = pattern.Modifiers?.ToList() ?? new List<string>()
                        });
                        if (clauseNumber > 0)
                        {
                            expression.Append(" OR ");
                        }
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                }
            }
            if (clauses.Any())
            {
                expression.Append(')');
            }
            else
            {
                return new ConvertedOatRule(rule.Id, rule);
            }
            foreach (var condition in rule.Conditions)
            {
                if (condition.Pattern?.Pattern != null)
                {
                    if (condition.SearchIn is null || condition.SearchIn.Equals("finding-only", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            FindingOnly = true,
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.StartsWith("finding-region", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var argList = new List<int>();
                        Match m = searchInRegex.Match(condition.SearchIn);
                        if (m.Success)
                        {
                            for (int i = 1; i < m.Groups.Count; i++)
                            {
                                if (int.TryParse(m.Groups[i].Value, out int value))
                                {
                                    argList.Add(value);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        if (argList.Count == 2)
                        {
                            clauses.Add(new WithinClause()
                            {
                                Data = new List<string>() { condition.Pattern.Pattern },
                                Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                                Invert = condition.NegateFinding,
                                Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                                Before = argList[0],
                                After = argList[1]
                            });
                            expression.Append(" AND ");
                            expression.Append(clauseNumber);
                            clauseNumber++;
                        }
                    }
                    else if (condition.SearchIn.Equals("same-line", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            SameLineOnly = true
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.Equals("same-file", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            SameFile = true
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.Equals("only-before", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            OnlyBefore = true
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.Equals("only-after", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            OnlyAfter = true
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                }
            }
            return new ConvertedOatRule(rule.Id, rule)
            {
                Clauses = clauses,
                Expression = expression.ToString()
            };
        }

        public IEnumerable<ConvertedOatRule> GetAllOatRules()
        {
            return _oatRules;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns> Enumerator </returns>
        public IEnumerator GetEnumerator()
        {
            return this._oatRules.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the Ruleset
        /// </summary>
        /// <returns> Enumerator </returns>
        IEnumerator<ConvertedOatRule> IEnumerable<ConvertedOatRule>.GetEnumerator()
        {
            return this._oatRules.GetEnumerator();
        }

        internal IEnumerable<Rule> StringToRules(string jsonstring, string sourcename, string? tag = null)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Error = HandleDeserializationError,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            List<Rule>? ruleList = JsonConvert.DeserializeObject<List<Rule>>(jsonstring, settings);
            if (ruleList is List<Rule>)
            {
                foreach (Rule r in ruleList)
                {
                    r.Source = sourcename;
                    r.RuntimeTag = tag;

                    foreach (SearchPattern pattern in r.Patterns)
                    {
                        SanitizePatternRegex(pattern);
                    }

                    foreach (SearchCondition condition in r.Conditions)
                    {
                        if (condition.Pattern is { })
                        {
                            SanitizePatternRegex(condition.Pattern);
                        }
                    }

                    yield return r;
                }
            }
        }

        private List<ConvertedOatRule> _oatRules;
        private Regex searchInRegex = new Regex("\\((.*),(.*)\\)", RegexOptions.Compiled);

        /// <summary>
        ///     Handler for deserialization error
        /// </summary>
        /// <param name="sender"> Sender object </param>
        /// <param name="errorArgs"> Error arguments </param>
        private void HandleDeserializationError(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            OnDeserializationErrorEventHandler?.Invoke(sender, errorArgs);
        }

        /// <summary>
        ///     Method santizes pattern to be a valid regex
        /// </summary>
        /// <param name="pattern"> </param>
        private static void SanitizePatternRegex(SearchPattern pattern)
        {
            if (pattern.PatternType == PatternType.RegexWord)
            {
                pattern.PatternType = PatternType.Regex;
                pattern.Pattern = string.Format(CultureInfo.InvariantCulture, @"\b({0})\b", pattern.Pattern ?? string.Empty);
            }
            else if (pattern.PatternType == PatternType.String)
            {
                pattern.PatternType = PatternType.Regex;
                pattern.Pattern = string.Format(CultureInfo.InvariantCulture, @"\b{0}\b", Regex.Escape(pattern.Pattern ?? string.Empty));
            }
            else if (pattern.PatternType == PatternType.Substring)
            {
                pattern.PatternType = PatternType.Regex;
                pattern.Pattern = string.Format(CultureInfo.InvariantCulture, @"{0}", Regex.Escape(pattern.Pattern ?? string.Empty));
            }
        }
    }
}
