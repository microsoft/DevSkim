// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License.
using KellermanSoftware.CompareNetObjects;
using Microsoft.CST.OAT.Captures;
using Microsoft.CST.OAT.Utils;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.CST.OAT
{
    /// <summary>
    /// This is the core engine of OAT
    /// </summary>
    public class Analyzer
    {
        private readonly ConcurrentDictionary<string, Regex?> RegexCache = new ConcurrentDictionary<string, Regex?>();

        /// <summary>
        /// The constructor for Analyzer takes no arguments.
        /// </summary>
        public Analyzer()
        {
            EqualsOperationDelegate = EqualsOperation;
            LessThanOperationDelegate = LessThanOperation;
            GreaterThanOperationDelegate = GreaterThanOperation;
            RegexOperationDelegate = RegexOperation;
            ContainsOperationDelegate = ContainsOperation;
            ContainsAnyOperationDelegate = ContainsAnyOperation;
            WasModifiedOperationDelegate = WasModifiedOperation;
            EndsWithOperationDelegate = EndsWithOperation;
            StartsWithOperationDelegate = StartsWithOperation;
            IsNullOperationDelegate = IsNullOperation;
            IsTrueOperationDelegate = IsTrueOperation;
            IsAfterOperationDelegate = IsAfterOperation;
            IsBeforeOperationDelegate = IsBeforeOperation;
            IsExpiredOperationDelegate = IsExpiredOperation;
            ContainsKeyOperationDelegate = ContainsKeyOperation;
        }

        /// <summary>
        /// This delegate is for iterating into complex objects like dictionaries that the Analyzer doesn't natively understand
        /// </summary>
        /// <param name="obj">Target object</param>
        /// <param name="index">String based index into the object</param>
        /// <returns>(If we successfully extracted, The extraction result)</returns>
        public delegate (bool Processed, object? Result) PropertyExtractionDelegate(object? obj, string index);

        /// <summary>
        /// This delegate is for turning complex objects like dictionaries that the Analyzer doesn't natively support into a dictionary or list of strings that OAT can use for default operations
        /// </summary>
        /// <param name="obj">Target object</param>
        /// <returns>(If the object was parsed, A list of Strings that were extracted, A List of KVP that were extracted)</returns>
        public delegate (bool Processed, IEnumerable<string> valsExtracted, IEnumerable<KeyValuePair<string, string>> dictExtracted) ObjectToValuesDelegate(object? obj);

        /// <summary>
        /// This delegate allows extending the Analyzer with a custom operation.
        /// </summary>
        /// <param name="clause">The clause being applied</param>
        /// <param name="state1">The first object state</param>
        /// <param name="state2">The second object state</param>
        /// <param name="captures">The previously found clause captures</param>
        /// <returns>(If the Operation delegate applies to the clause, If the operation was successful, if capturing is enabled the ClauseCapture)</returns>
        public delegate (bool Applies, bool Result, ClauseCapture? Capture) OperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures);

        /// <summary>
        /// Default Operation Delegates don't return applies
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public delegate (bool Result, ClauseCapture? Capture) BuiltinOperationDelegate(Clause clause, object? state1, object? state2);

        /// <summary>
        /// This delegate allows extending the Analyzer with extra rule validation for custom rules.
        /// </summary>
        /// <param name="r">The Target Rule</param>
        /// <param name="c">The Target Clause</param>
        /// <returns>(If the validation applied, The Enumerable of Violations found)</returns>
        public delegate (bool Applies, IEnumerable<Violation> FoundViolations) ValidationDelegate(Rule r, Clause c);

        /// <summary>
        /// The PropertyExtractionDelegates that will be used in order of attempt.  Once successful the others won't be run.
        /// </summary>
        public List<PropertyExtractionDelegate> CustomPropertyExtractionDelegates { get; set; } = new List<PropertyExtractionDelegate>();

        /// <summary>
        /// The ObjectToValuesDelegates that will be used in order of attempt. Once successful the others won't be run.
        /// </summary>
        public List<ObjectToValuesDelegate> CustomObjectToValuesDelegates { get; set; } = new List<ObjectToValuesDelegate>();

        /// <summary>
        /// The OperationDelegates that will be used in order of attempt.  Once successful the others won't be run.
        /// </summary>
        public List<OperationDelegate> CustomOperationDelegates { get; set; } = new List<OperationDelegate>();

        /// <summary>
        /// The EQ Operation Delegate. Set to override EQ behavior.
        /// </summary>
        public BuiltinOperationDelegate EqualsOperationDelegate { get; set; }
        /// <summary>
        /// The LT Operation Delegate. Set to override LT behavior.
        /// </summary>
        public BuiltinOperationDelegate LessThanOperationDelegate { get; set; }
        /// <summary>
        /// The GT Operation Delegate. Set to override GT behavior.
        /// </summary>
        public BuiltinOperationDelegate GreaterThanOperationDelegate { get; set; }
        /// <summary>
        /// The REGEX Operation Delegate. Set to override REGEX behavior.
        /// </summary>
        public BuiltinOperationDelegate RegexOperationDelegate { get; set; }
        /// <summary>
        /// The CONTAINS Operation Delegate. Set to override CONTAINS behavior.
        /// </summary>
        public BuiltinOperationDelegate ContainsOperationDelegate { get; set; }
        /// <summary>
        /// The CONTAINS_ANY Operation Delegate. Set to override CONTAINS_ANY behavior.
        /// </summary>
        public BuiltinOperationDelegate ContainsAnyOperationDelegate { get; set; }
        /// <summary>
        /// The WAS_MODIFIED Operation Delegate. Set to override WAS_MODIFIED behavior.
        /// </summary>
        public BuiltinOperationDelegate WasModifiedOperationDelegate { get; set; }
        /// <summary>
        /// The ENDS_WITH Operation Delegate. Set to override ENDS_WITH behavior.
        /// </summary>
        public BuiltinOperationDelegate EndsWithOperationDelegate { get; set; }
        /// <summary>
        /// The STARTS_WITH Operation Delegate. Set to override STARTS_WITH behavior.
        /// </summary>
        public BuiltinOperationDelegate StartsWithOperationDelegate { get; set; }
        /// <summary>
        /// The IS_NULL Operation Delegate. Set to override IS_NULL behavior.
        /// </summary>
        public BuiltinOperationDelegate IsNullOperationDelegate { get; set; }
        /// <summary>
        /// The IS_TRUE Operation Delegate. Set to override IS_TRUE behavior.
        /// </summary>
        public BuiltinOperationDelegate IsTrueOperationDelegate { get; set; }
        /// <summary>
        /// The IS_AFTER Operation Delegate. Set to override IS_AFTER behavior.
        /// </summary>
        public BuiltinOperationDelegate IsAfterOperationDelegate { get; set; }
        /// <summary>
        /// The IS_BEFORE Operation Delegate. Set to override IS_BEFORE behavior.
        /// </summary>
        public BuiltinOperationDelegate IsBeforeOperationDelegate { get; set; }
        /// <summary>
        /// The IS_EXPIRED Operation Delegate. Set to override IS_EXPIRED behavior.
        /// </summary>
        public BuiltinOperationDelegate IsExpiredOperationDelegate { get; set; }
        /// <summary>
        /// The CONTAINS_KEY Operation Delegate. Set to override CONTAINS_KEY behavior.
        /// </summary>
        public BuiltinOperationDelegate ContainsKeyOperationDelegate { get; set; }

        /// <summary>
        /// The ValidationDelegates that will be used in order of attempt when encountering a CustomOperation in EnumerateRuleIssues.
        /// All will be run. Order not guaranteed.
        /// </summary>
        public List<ValidationDelegate> CustomOperationValidationDelegates { get; set; } = new List<ValidationDelegate>();

        /// <summary>
        /// Extracts a value stored at the specified path inside an object. Can crawl into Lists and
        /// Dictionaries of strings and return any top-level object.
        /// </summary>
        /// <param name="targetObject">The object to parse</param>
        /// <param name="pathToProperty">The path of the property to fetch</param>
        /// <returns>The object found</returns>
        public object? GetValueByPropertyString(object? targetObject, string pathToProperty)
        {
            if (pathToProperty is null || targetObject is null)
            {
                return null;
            }
            try
            {
                var pathPortions = pathToProperty.Split('.');

                // We first try to get the first value to get it started
                var value = GetValueByPropertyOrFieldName(targetObject, pathPortions[0]);

                // For the rest of the path we walk each portion to get the next object
                for (int pathPortionIndex = 1; pathPortionIndex < pathPortions.Length; pathPortionIndex++)
                {
                    if (value == null) { break; }

                    switch (value)
                    {
                        case Dictionary<string, string> stringDict:
                            if (stringDict.TryGetValue(pathPortions[pathPortionIndex], out string? stringValue))
                            {
                                value = stringValue;
                            }
                            else
                            {
                                value = null;
                            }
                            break;

                        case List<string> stringList:
                            if (int.TryParse(pathPortions[pathPortionIndex], out int ArrayIndex) && stringList.Count > ArrayIndex)
                            {
                                value = stringList[ArrayIndex];
                            }
                            else
                            {
                                value = null;
                            }
                            break;

                        default:
                            (bool Processed, object? Result)? res = null;
                            var found = false;
                            foreach (var del in CustomPropertyExtractionDelegates)
                            {
                                res = del?.Invoke(value, pathPortions[pathPortionIndex]);
                                if (res.HasValue && res.Value.Processed)
                                {
                                    found = true;
                                    value = res.Value.Result;
                                    break;
                                }
                            }

                            // If we couldn't do any custom parsing fall back to the default
                            if (!found)
                            {
                                value = GetValueByPropertyOrFieldName(value, pathPortions[pathPortionIndex]);
                            }
                            break;
                    }
                }
                return value;
            }
            catch (Exception e)
            {
                Log.Information("Fetching Field {0} failed from {1} ({2}:{3})", pathToProperty, targetObject.GetType(), e.GetType(), e.Message);
            }
            return null;
        }

        /// <summary>
        ///     Prints out the Enumerable of violations to Warning
        /// </summary>
        /// <param name="violations">An Enumerable of Violations to print</param>
        public static void PrintViolations(IEnumerable<Violation> violations)
        {
            if (violations == null) return;
            foreach (var violation in violations)
            {
                Log.Warning(violation.Description);
            }
        }

        /// <summary>
        ///     Get the Tags which apply to the object given the Rules
        /// </summary>
        /// <param name="rules">The Rules to apply</param>
        /// <param name="state1">The first state of the object</param>
        /// <param name="state2">The second state of the object</param>
        /// <returns></returns>
        public string[] GetTags(IEnumerable<Rule> rules, object? state1 = null, object? state2 = null)
        {
            var tags = new ConcurrentDictionary<string, byte>();

            Parallel.ForEach(rules, rule =>
            {
                // If there are no tags, or all of the tags are already in the tags we've found skip otherwise apply.
                if ((!rule.Tags.Any() || !rule.Tags.All(x => tags.Keys.Any(y => y == x))) && Applies(rule, state1, state2))
                {
                    foreach (var tag in rule.Tags)
                    {
                        tags.TryAdd(tag, 0);
                    }
                }
            });

            return tags.Keys.ToArray();
        }

        /// <summary>
        /// Get the RuleCaptures for the List of rules as applied to the objects
        /// </summary>
        /// <param name="rules">List of Rules to run</param>
        /// <param name="state1">First state of object</param>
        /// <param name="state2">Second state of object</param>
        /// <returns></returns>
        public IEnumerable<RuleCapture> GetCaptures(IEnumerable<Rule> rules, object? state1 = null, object? state2 = null)
        {
            var results = new ConcurrentStack<RuleCapture>();

            Parallel.ForEach(rules, rule =>
            {
                var captured = GetCapture(rule, state1, state2);
                if (captured.RuleMatches && captured.Result != null)
                {
                    results.Push(captured.Result);
                }
            });

            return results;
        }

        /// <summary>
        /// Checks if the Rule matches and obtains its Capture
        /// </summary>
        /// <param name="rule">The Rule to test</param>
        /// <param name="state1">object state1</param>
        /// <param name="state2">object state2</param>
        /// <returns></returns>
        public (bool RuleMatches, RuleCapture? Result) GetCapture(Rule rule, object? state1 = null, object? state2 = null)
        {
            if (rule != null)
            {
                var ruleCapture = new RuleCapture(rule, new List<ClauseCapture>());
                var sample = state1 is null ? state2 : state1;

                // Does the name of this class match the Target in the rule?
                // Or has no target been specified (match all)
                if (rule.Target is null || (sample?.GetType().Name.Equals(rule.Target, StringComparison.InvariantCultureIgnoreCase) ?? true))
                {
                    // If the expression is null the default is that all clauses must be true
                    // If we have no clauses .All will still match
                    if (rule.Expression is null)
                    {
                        foreach (var clause in rule.Clauses)
                        {
                            var (ClauseMatches, ClauseCapture) = GetClauseCapture(clause, state1, state2, ruleCapture.Captures);
                            if (ClauseMatches)
                            {
                                if (ClauseCapture != null)
                                {
                                    ruleCapture.Captures.Add(ClauseCapture);
                                }
                            }
                            else
                            {
                                return (false, null);
                            }
                        }
                        return (true, ruleCapture);
                    }
                    // Otherwise we evaluate the expression
                    else
                    {
                        var (ExpressionMatches, Captures) = Evaluate(rule.Expression.Split(' '), rule.Clauses, state1, state2, ruleCapture.Captures);
                        if (ExpressionMatches)
                        {
                            ruleCapture.Captures.AddRange(Captures);
                            return (true, ruleCapture);
                        }
                    }
                }
            }
            return (false, null);
        }

        /// <summary>
        ///     Which rules apply to this object given up to two states?
        /// </summary>
        /// <param name="rules">The rules to apply</param>
        /// <param name="state1">The first state</param>
        /// <param name="state2">The second state</param>
        /// <returns>A Stack of Rules which apply</returns>
        public IEnumerable<Rule> Analyze(IEnumerable<Rule> rules, object? state1 = null, object? state2 = null)
        {
            var results = new ConcurrentStack<Rule>();

            Parallel.ForEach(rules, rule =>
            {
                if (Applies(rule, state1, state2))
                {
                    results.Push(rule);
                }
            });

            return results;
        }

        /// <summary>
        ///     Does the rule apply to the object?
        /// </summary>
        /// <param name="rule">The Rule to apply</param>
        /// <param name="state1">The first state of the object</param>
        /// <param name="state2">The second state of the object</param>
        /// <returns>True if the rule applies</returns>
        public bool Applies(Rule rule, object? state1 = null, object? state2 = null)
        {
            if (rule != null)
            {
                var sample = state1 is null ? state2 : state1;

                // Does the name of this class match the Target in the rule?
                // Or has no target been specified (match all)
                if (rule.Target is null || (sample?.GetType().Name.Equals(rule.Target, StringComparison.InvariantCultureIgnoreCase) ?? true))
                {
                    // If the expression is null the default is that all clauses must be true
                    // If we have no clauses .All will still match
                    if (rule.Expression is null)
                    {
                        if (rule.Clauses.All(x => AnalyzeClause(x, state1, state2)))
                        {
                            return true;
                        }
                    }
                    // Otherwise we evaluate the expression
                    else
                    {
                        var result = Evaluate(rule.Expression.Split(' '), rule.Clauses, state1, state2);
                        if (result.Success)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            else
            {
                throw new NullReferenceException();
            }
        }

        /// <summary>
        /// Determines if there are any problems with the provided rule.
        /// </summary>
        /// <param name="rule">The rule to parse.</param>
        /// <returns>True if there are no issues.</returns>
        public bool IsRuleValid(Rule rule) => !EnumerateRuleIssues(new Rule[] { rule }).Any();

        /// <summary>
        /// Verifies the provided rules and provides a list of issues with the rules.
        /// </summary>
        /// <param name="rules">Enumerable of Rules.</param>
        /// <returns>Enumerable of issues with the rules.</returns>
        public IEnumerable<Violation> EnumerateRuleIssues(IEnumerable<Rule> rules)
        {
            if (!Strings.IsLoaded)
            {
                Strings.Setup();
            }
            foreach (Rule rule in rules ?? Array.Empty<Rule>())
            {
                var clauseLabels = rule.Clauses.GroupBy(x => x.Label);

                // If clauses have duplicate names
                foreach (var duplicateClause in clauseLabels.Where(x => x.Key != null && x.Count() > 1))
                {
                    yield return new Violation(string.Format(Strings.Get("Err_ClauseDuplicateName"), rule.Name, duplicateClause.Key ?? string.Empty), rule, duplicateClause.AsEnumerable().ToArray());
                }

                // If clause label contains illegal characters
                foreach (var clause in rule.Clauses)
                {
                    if (clause.Label is string label)
                    {
                        if (label.Contains(" ") || label.Contains("(") || label.Contains(")"))
                        {
                            yield return new Violation(string.Format(Strings.Get("Err_ClauseInvalidLabel"), rule.Name, label), rule, clause);
                        }
                    }
                    switch (clause.Operation)
                    {
                        case Operation.Equals:
                            if ((clause.Data?.Count == null || clause.Data?.Count == 0))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            if (clause.DictData != null || clause.DictData?.Count > 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
                            }

                            break;

                        case Operation.Contains:
                        case Operation.ContainsAny:
                            if ((clause.Data?.Count == null || clause.Data?.Count == 0) && (clause.DictData?.Count == null || clause.DictData?.Count == 0))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoDataOrDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            if ((clause.Data is List<string> list && list.Count > 0) && (clause.DictData is List<KeyValuePair<string, string>> dictList && dictList.Count > 0))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseBothDataDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            break;

                        case Operation.EndsWith:
                        case Operation.StartsWith:
                            if (clause.Data?.Count == null || clause.Data?.Count == 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            if (clause.DictData != null || clause.DictData?.Count > 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
                            }
                            break;

                        case Operation.GreaterThan:
                        case Operation.LessThan:
                            if (clause.Data?.Count == null || clause.Data is List<string> clauseList && (clauseList.Count != 1 || !int.TryParse(clause.Data.First(), out int _)))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseExpectedInt"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            if (clause.DictData != null || clause.DictData?.Count > 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
                            }
                            break;

                        case Operation.Regex:
                            if (clause.Data?.Count == null || clause.Data?.Count == 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            else if (clause.Data is List<string> regexList)
                            {
                                foreach (var regex in regexList)
                                {
                                    if (!Helpers.IsValidRegex(regex))
                                    {
                                        yield return new Violation(string.Format(Strings.Get("Err_ClauseInvalidRegex"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), regex), rule, clause);
                                    }
                                }
                            }
                            if (clause.DictData != null || clause.DictData?.Count > 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
                            }
                            break;

                        case Operation.IsNull:
                        case Operation.IsTrue:
                        case Operation.IsExpired:
                        case Operation.WasModified:
                            if (!(clause.Data?.Count == null || clause.Data?.Count == 0))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseRedundantData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            else if (!(clause.DictData?.Count == null || clause.DictData?.Count == 0))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseRedundantDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            break;

                        case Operation.IsBefore:
                        case Operation.IsAfter:
                            if (clause.Data?.Count == null || clause.Data is List<string> clauseList2 && (clauseList2.Count != 1 || !DateTime.TryParse(clause.Data.First(), out DateTime _)))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseExpectedDateTime"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            if (clause.DictData != null || clause.DictData?.Count > 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
                            }
                            break;

                        case Operation.ContainsKey:
                            if (clause.DictData != null)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseUnexpectedDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            if (clause.Data == null || clause.Data?.Count == 0)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseMissingListData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            break;

                        case Operation.Custom:
                            if (clause.CustomOperation == null)
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseMissingCustomOperation"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                            }
                            else
                            {
                                bool covered = false;
                                foreach (var del in CustomOperationValidationDelegates)
                                {
                                    var res = del?.Invoke(rule, clause) ?? (false, new List<Violation>());
                                    if (res.Applies)
                                    {
                                        covered = true;
                                        foreach (var violation in res.FoundViolations)
                                        {
                                            yield return violation;
                                        }
                                    }
                                }
                                if (!covered)
                                {
                                    yield return new Violation(string.Format(Strings.Get("Err_ClauseMissingValidationForOperation"), clause.CustomOperation, rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
                                }
                            }
                            break;

                        default:
                            yield return new Violation(string.Format(Strings.Get("Err_ClauseUnsuppportedOperator"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
                            break;
                    }
                }

                var foundLabels = new List<string>();

                if (rule.Expression is string expression)
                {
                    // Are parenthesis balanced Are spaces correct Are all variables defined by
                    // clauses? Are variables and operators alternating?
                    var splits = expression.Split(' ');
                    int foundStarts = 0;
                    int foundEnds = 0;
                    bool expectingOperator = false;
                    for (int i = 0; i < splits.Length; i++)
                    {
                        foundStarts += splits[i].Count(x => x.Equals('('));
                        foundEnds += splits[i].Count(x => x.Equals(')'));
                        if (foundEnds > foundStarts)
                        {
                            yield return new Violation(string.Format(Strings.Get("Err_ClauseUnbalancedParentheses"), expression, rule.Name), rule);
                        }
                        // Variable
                        if (!expectingOperator)
                        {
                            var lastOpen = -1;
                            var lastClose = -1;

                            for (int j = 0; j < splits[i].Length; j++)
                            {
                                // Check that the parenthesis are balanced
                                if (splits[i][j] == '(')
                                {
                                    // If we've seen a ) this is now invalid
                                    if (lastClose != -1)
                                    {
                                        yield return new Violation(string.Format(Strings.Get("Err_ClauseParenthesisInLabel"), expression, rule.Name, splits[i]), rule);
                                    }
                                    // If there were any characters between open parenthesis
                                    if (j - lastOpen != 1)
                                    {
                                        yield return new Violation(string.Format(Strings.Get("Err_ClauseCharactersBetweenOpenParentheses"), expression, rule.Name, splits[i]), rule);
                                    }
                                    // If there was a random parenthesis not starting the variable
                                    else if (j > 0)
                                    {
                                        yield return new Violation(string.Format(Strings.Get("Err_ClauseCharactersBeforeOpenParentheses"), expression, rule.Name, splits[i]), rule);
                                    }
                                    lastOpen = j;
                                }
                                else if (splits[i][j] == ')')
                                {
                                    // If we've seen a close before update last
                                    if (lastClose != -1 && j - lastClose != 1)
                                    {
                                        yield return new Violation(string.Format(Strings.Get("Err_ClauseCharactersBetweenClosedParentheses"), expression, rule.Name, splits[i]), rule);
                                    }
                                    lastClose = j;
                                }
                                else
                                {
                                    // If we've set a close this is invalid because we can't have
                                    // other characters after it
                                    if (lastClose != -1)
                                    {
                                        yield return new Violation(string.Format(Strings.Get("Err_ClauseCharactersAfterClosedParentheses"), expression, rule.Name, splits[i]), rule);
                                    }
                                }
                            }

                            var variable = splits[i].Replace("(", "").Replace(")", "");

                            if (variable == "NOT")
                            {
                                if (splits[i].Contains(")"))
                                {
                                    yield return new Violation(string.Format(Strings.Get("Err_ClauseCloseParenthesesInNot"), expression, rule.Name, splits[i]), rule);
                                }
                            }
                            else
                            {
                                foundLabels.Add(variable);
                                if (string.IsNullOrWhiteSpace(variable) || (!rule.Clauses.Any(x => x.Label == variable) && !(int.TryParse(variable, out int result) && result < rule.Clauses.Count)))
                                {
                                    yield return new Violation(string.Format(Strings.Get("Err_ClauseUndefinedLabel"), expression, rule.Name, splits[i].Replace("(", "").Replace(")", "")), rule);
                                }
                                expectingOperator = true;
                            }
                        }
                        //Operator
                        else
                        {
                            // If we can't enum parse the operator
                            if (!Enum.TryParse<BOOL_OPERATOR>(splits[i], out BOOL_OPERATOR op))
                            {
                                yield return new Violation(string.Format(Strings.Get("Err_ClauseInvalidOperator"), expression, rule.Name, splits[i]), rule);
                            }
                            // We don't allow NOT operators to modify other Operators, so we can't
                            // allow NOT here
                            else
                            {
                                if (op is BOOL_OPERATOR boolOp && boolOp == BOOL_OPERATOR.NOT)
                                {
                                    yield return new Violation(string.Format(Strings.Get("Err_ClauseInvalidNotOperator"), expression, rule.Name), rule);
                                }
                            }
                            expectingOperator = false;
                        }
                    }

                    // We should always end on expecting an operator (having gotten a variable)
                    if (!expectingOperator)
                    {
                        yield return new Violation(string.Format(Strings.Get("Err_ClauseEndsWithOperator"), expression, rule.Name), rule);
                    }
                }

                // Were all the labels declared in clauses used?
                foreach (var label in rule.Clauses.Select(x => x.Label))
                {
                    if (label is string)
                    {
                        if (!foundLabels.Contains(label))
                        {
                            yield return new Violation(string.Format(Strings.Get("Err_ClauseUnusedLabel"), label, rule.Name), rule);
                        }
                    }
                }

                var justTheLabels = clauseLabels.Select(x => x.Key);
                // If any clause has a label they all must have labels
                if (justTheLabels.Any(x => x is string) && justTheLabels.Any(x => x is null))
                {
                    yield return new Violation(string.Format(Strings.Get("Err_ClauseMissingLabels"), rule.Name), rule);
                }
                // If the clause has an expression it may not have any null labels
                if (rule.Expression != null && justTheLabels.Any(x => x is null))
                {
                    yield return new Violation(string.Format(Strings.Get("Err_ClauseExpressionButMissingLabels"), rule.Name), rule);
                }
            }
        }

        /// <summary>
        /// Determine if a Clause is true or false
        /// </summary>
        /// <param name="clause">The Clause to Analyze</param>
        /// <param name="state1">The first object state</param>
        /// <param name="state2">The second object state</param>
        /// <returns>If the Clause is true</returns>
        public bool AnalyzeClause(Clause clause, object? state1 = null, object? state2 = null)
        {
            if (clause == null)
            {
                return false;
            }

            try
            {
                var res = GetClauseCapture(clause, state1, state2);
                return res.Applies;
            }
            catch (Exception e)
            {
                Log.Debug(e, $"Hit while parsing {JsonConvert.SerializeObject(clause)} onto ({JsonConvert.SerializeObject(state1)},{JsonConvert.SerializeObject(state2)})");
            }
            return false;
        }

        private (bool Applies, ClauseCapture? Capture) GetClauseCapture(Clause clause, object? state1 = null, object? state2 = null, IEnumerable<ClauseCapture>? captures = null)
        {
            if (clause.Field is string)
            {
                state2 = GetValueByPropertyString(state2, clause.Field);
                state1 = GetValueByPropertyString(state1, clause.Field);
            }

            if (clause.Operation is Operation.Custom)
            {
                foreach (var del in CustomOperationDelegates)
                {
                    var res = del?.Invoke(clause, state1, state2, captures);
                    if (res.HasValue && res.Value.Applies)
                    {
                        return (res.Value.Result, !clause.Capture ? null : res.Value.Capture);
                    }
                }
                Log.Debug("Custom operation hit but delegate for {0} isn't set.", clause.CustomOperation);
                return (false, null);
            }

            BuiltinOperationDelegate func = clause.Operation switch
            {
                Operation.Equals => EqualsOperationDelegate,
                Operation.Contains => ContainsOperationDelegate,
                Operation.ContainsAny => ContainsAnyOperationDelegate,
                Operation.LessThan => LessThanOperationDelegate,
                Operation.GreaterThan => GreaterThanOperationDelegate,
                Operation.Regex => RegexOperationDelegate,
                Operation.WasModified => WasModifiedOperationDelegate,
                Operation.EndsWith => EndsWithOperationDelegate,
                Operation.StartsWith => StartsWithOperationDelegate,
                Operation.IsNull => IsNullOperationDelegate,
                Operation.IsTrue => IsTrueOperationDelegate,
                Operation.IsBefore => IsBeforeOperationDelegate,
                Operation.IsAfter => IsAfterOperationDelegate,
                Operation.IsExpired => IsExpiredOperationDelegate,
                Operation.ContainsKey => ContainsKeyOperationDelegate,
                _ => NopOperation
            };

            return func.Invoke(clause, state1, state2);
        }

        private static int FindMatchingParen(string[] splits, int startingIndex)
        {
            int foundStarts = 0;
            int foundEnds = 0;
            for (int i = startingIndex; i < splits.Length; i++)
            {
                foundStarts += splits[i].Count(x => x.Equals('('));
                foundEnds += splits[i].Count(x => x.Equals(')'));

                if (foundStarts <= foundEnds)
                {
                    return i;
                }
            }

            return splits.Length - 1;
        }

        /// <summary>
        /// Gets the object value stored at the field or property named by the string. Property tried first.  Returns null if none found.
        /// </summary>
        /// <param name="obj">The target object</param>
        /// <param name="propertyName">The Property or Field name</param>
        /// <returns>The object at that Name or null</returns>
        public static object? GetValueByPropertyOrFieldName(object? obj, string? propertyName) => obj?.GetType().GetProperty(propertyName ?? string.Empty)?.GetValue(obj) ?? obj?.GetType().GetField(propertyName ?? string.Empty)?.GetValue(obj);

        internal (bool Result, ClauseCapture? Capture) RegexOperation(Clause clause, object? state1, object? state2)
        {
            (var stateOneList, _) = ObjectToValues(state1);
            (var stateTwoList, _) = ObjectToValues(state2);
            if (clause.Data is List<string> RegexList && RegexList.Any())
            {
                var built = string.Join("|", RegexList);

                var regex = StringToRegex(built);

                if (regex != null)
                {
                    foreach (var state in stateOneList)
                    {
                        var matches = regex.Matches(state);

                        if (matches.Count > 0 || (matches.Count == 0 && clause.Invert))
                        {
                            var outmatches = new List<Match>();
                            foreach(var match in matches)
                            {
                                if (match is Match m)
                                {
                                    outmatches.Add(m);
                                }
                            }
                            return (true, !clause.Capture ? null : new TypedClauseCapture<List<Match>>(clause, outmatches, state1));
                        }
                    }
                    foreach (var state in stateTwoList)
                    {
                        var matches = regex.Matches(state);

                        if (matches.Count > 0 || (matches.Count == 0 && clause.Invert))
                        {
                            var outmatches = new List<Match>();
                            foreach (var match in matches)
                            {
                                if (match is Match m)
                                {
                                    outmatches.Add(m);
                                }
                            }
                            return (true, !clause.Capture ? null : new TypedClauseCapture<List<Match>>(clause, outmatches, state2: state2));
                        }
                    }
                }
            }
            return (false, null);
        }

        /// <summary>
        /// Returns the compiled regex for a string.  Backed by an internal cache to make subsequent uses of the same expression fast.
        /// </summary>
        /// <param name="built"></param>
        /// <returns>The created Regex or null on error</returns>
        public Regex? StringToRegex(string built)
        {
            if (!RegexCache.ContainsKey(built))
            {
                try
                {
                    RegexCache.TryAdd(built, new Regex(built, RegexOptions.Compiled));
                }
                catch (ArgumentException)
                {
                    Log.Warning("InvalidArgumentException when creating regex. Regex {0} is invalid and will be skipped.", built);
                    RegexCache.TryAdd(built, null);
                }
            }
            return RegexCache[built];
        }

        internal (bool Result, ClauseCapture? Capture) ContainsAnyOperation(Clause clause, object? state1, object? state2)
        {
            var typeHolder = state1 ?? state2;

            if (typeHolder?.GetType().IsDefined(typeof(FlagsAttribute), false) is true)
            {
                bool ParseContainsAnyEnum(Enum state)
                {
                    foreach (var datum in clause.Data ?? new List<string>())
                    {
                        #if !NETSTANDARD2_0
                        if (Enum.TryParse(typeHolder.GetType(), datum, out object result))
                        {
                            if (result is Enum eresult)
                            {
                                if (state.HasFlag(eresult))
                                {
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                        #else
                        try
                        {
                            var result = Enum.Parse(typeHolder.GetType(), datum);
                            if (state.HasFlag(result as Enum))
                            {
                                return true;
                            }
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                        #endif
                    }
                    return false;
                }

                if (state1 is Enum enum1)
                {
                    var res = ParseContainsAnyEnum(enum1);
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        return (true, !clause.Capture ? null : new TypedClauseCapture<Enum>(clause, enum1, state1, null));
                    }
                }
                if (state2 is Enum enum2)
                {
                    var res = ParseContainsAnyEnum(enum2);
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        return (true, !clause.Capture ? null : new TypedClauseCapture<Enum>(clause, enum2, null, state2));
                    }
                }

                return (false, null);
            }

            (var stateOneList, var stateOneDict) = ObjectToValues(state1);
            (var stateTwoList, var stateTwoDict) = ObjectToValues(state2);

            if (clause.DictData is List<KeyValuePair<string, string>> ContainsData)
            {
                if (stateOneDict.Any())
                {
                    var captured = new List<KeyValuePair<string, string>>();
                    foreach (var entry in stateOneDict)
                    {
                        var res = ContainsData.Contains(entry);
                        if ((res && !clause.Invert) || (clause.Invert && !res))
                        {
                            captured.Add(entry);
                        }
                    }

                    if (captured.Any())
                    {
                        var returnVal = clause.Capture ?
                            new TypedClauseCapture<List<KeyValuePair<string, string>>>(clause, captured, state1, null) :
                            null;
                        return (true, returnVal);
                    }
                }
                if (stateTwoDict.Any())
                {
                    var captured = new List<KeyValuePair<string, string>>();
                    foreach (var entry in stateTwoDict)
                    {
                        var res = ContainsData.Contains(entry);
                        if ((res && !clause.Invert) || (clause.Invert && !res))
                        {
                            captured.Add(entry);
                        }
                    }

                    if (captured.Any())
                    {
                        var returnVal = clause.Capture ?
                            new TypedClauseCapture<List<KeyValuePair<string, string>>>(clause, captured, null, state2) :
                            null;
                        return (true, returnVal);
                    }
                }
                return (false, null);
            }

            if (clause.Data is List<string> ClauseData)
            {
                (bool Applies, List<string>? Matches) ClauseAppliesToList(List<string> stateList)
                {
                    // If we are dealing with an array on the object side
                    if (typeHolder is List<string>)
                    {
                        var foundStates = new List<string>();
                        foreach (var entry in stateList)
                        {
                            if ((!clause.Invert && ClauseData.Contains(entry)) || (clause.Invert && !ClauseData.Contains(entry)))
                            {
                                foundStates.Add(entry);
                            }
                        }
                        if (foundStates.Count == 0)
                        {
                            return (false, null);
                        }

                        return (true, foundStates);
                    }
                    // If we are dealing with a single string we do a .Contains instead
                    else if (typeHolder is string)
                    {
                        var results = new List<string>();
                        foreach (var datum in stateList)
                        {
                            if (clause.Data.Any(x => (clause.Invert && !datum.Contains(x)) || (!clause.Invert && datum.Contains(x))))
                            {
                                results.Add(datum);
                            }
                        }
                        return (results.Any(), clause.Capture ? results : null);
                    }
                    return (false, new List<string>());
                }

                var result = ClauseAppliesToList(stateOneList);
                if (result.Applies)
                {
                    if (result.Matches?.Any() is true)
                    {
                        return typeHolder switch
                        {
                            string _ => (true, new TypedClauseCapture<string>(clause, result.Matches.First(), state1, null)),
                            _ => (true, new TypedClauseCapture<List<string>>(clause, result.Matches, state1, null)),
                        };
                    }
                    else
                    {
                        return (true, null);
                    }
                }
                result = ClauseAppliesToList(stateTwoList);
                if (result.Applies)
                {
                    if (result.Matches?.Any() is true)
                    {
                        return typeHolder switch
                        {
                            string _ => (true, new TypedClauseCapture<string>(clause, result.Matches.First(), null, state2)),
                            _ => (true, new TypedClauseCapture<List<string>>(clause, result.Matches, null, state2)),
                        };
                    }
                    else
                    {
                        return (true, null);
                    }
                }
            }

            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) ContainsOperation(Clause clause, object? state1, object? state2)
        {
            var typeHolder = state1 ?? state2;

            if (typeHolder?.GetType().IsDefined(typeof(FlagsAttribute), false) is true)
            {
                bool ParseContainsAllEnum(Enum state)
                {
                    foreach (var datum in clause.Data ?? new List<string>())
                    {
#if !NETSTANDARD2_0
                        if (Enum.TryParse(typeHolder.GetType(), datum, out object result))
                        {
                            if (result is Enum eresult)
                            {
                                if (!state.HasFlag(eresult))
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
#else
                        try
                        {
                            var result = Enum.Parse(typeHolder.GetType(), datum);
                            if (!state.HasFlag(result as Enum))
                            {
                                return false;
                            }
                        }
                        catch (Exception)
                        {
                            return false;
                        }
#endif
                    }
                    return true;
                }

                if (state1 is Enum enum1)
                {
                    var res = ParseContainsAllEnum(enum1);
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        return (true, !clause.Capture ? null : new TypedClauseCapture<Enum>(clause, enum1, state1, null));
                    }
                }
                if (state2 is Enum enum2)
                {
                    var res = ParseContainsAllEnum(enum2);
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        return (true, !clause.Capture ? null : new TypedClauseCapture<Enum>(clause, enum2, null, state2));
                    }
                }

                return (false, null);
            }

            (var stateOneList, var stateOneDict) = ObjectToValues(state1);
            (var stateTwoList, var stateTwoDict) = ObjectToValues(state2);

            if (clause.DictData is List<KeyValuePair<string, string>> ContainsData)
            {
                if (stateOneDict.Any())
                {
                    var res = stateOneDict.Where(x => (!clause.Invert && ContainsData.Contains(x)) || (clause.Invert && !ContainsData.Contains(x)));
                    if (res.Any())
                    {
                        var captured = clause.Capture ?
                            new TypedClauseCapture<List<KeyValuePair<string, string>>>(clause, res.ToList(), state1, null) :
                            null;
                        return (true, captured);
                    }
                }
                if (stateTwoDict.Any())
                {
                    var res = stateTwoDict.Where(x => (!clause.Invert && ContainsData.Contains(x)) || (clause.Invert && !ContainsData.Contains(x)));
                    if (res.Any())
                    {
                        var captured = clause.Capture ?
                            new TypedClauseCapture<List<KeyValuePair<string, string>>>(clause, res.ToList(), null, state2) :
                            null;
                        return (true, captured);
                    }
                }
                return (false, null);
            }

            if (clause.Data is List<string> ClauseData)
            {
                (bool Applies, List<string>? Matches) ClauseAppliesToList(List<string> stateList)
                {
                    // If we are dealing with an array on the object side
                    if (typeHolder is List<string>)
                    {
                        var res = stateList.Where(x => (!clause.Invert && clause.Data.Contains(x)) || (clause.Invert && !clause.Data.Contains(x)));
                        if (res.Any())
                        {
                            return (true, clause.Capture ? res.ToList() : null);
                        }
                    }
                    // If we are dealing with a single string we do a .Contains instead
                    else if (typeHolder is string)
                    {
                        var results = new List<string>();
                        foreach (var datum in stateList)
                        {
                            var res = clause.Data.Where(x => (clause.Invert && !datum.Contains(x) || (!clause.Invert && datum.Contains(x))));
                            if (res.Any())
                            {
                                results.Add(datum);
                            }
                        }
                        return (results.Any(), clause.Capture ? results : null);
                    }
                    return (false, new List<string>());
                }

                var result = ClauseAppliesToList(stateOneList);
                if (result.Applies)
                {
                    if (result.Matches?.Any() is true)
                    {
                        return typeHolder switch
                        {
                            string _ => (true, new TypedClauseCapture<string>(clause, result.Matches.First(), state1, null)),
                            _ => (true, new TypedClauseCapture<List<string>>(clause, result.Matches, state1, null)),
                        };
                    }
                    else
                    {
                        return (true, null);
                    }
                }
                result = ClauseAppliesToList(stateTwoList);
                if (result.Applies)
                {
                    if (result.Matches?.Any() is true)
                    {
                        return typeHolder switch
                        {
                            string _ => (true, new TypedClauseCapture<string>(clause, result.Matches.First(), null, state2)),
                            _ => (true, new TypedClauseCapture<List<string>>(clause, result.Matches, null, state2)),
                        };
                    }
                    else
                    {
                        return (true, null);
                    }
                }
            }

            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) GreaterThanOperation(Clause clause, object? state1, object? state2)
        {
            (var stateOneList, _) = ObjectToValues(state1);
            (var stateTwoList, _) = ObjectToValues(state2);

            foreach (var val in stateOneList)
            {
                if (int.TryParse(val, out int valToCheck)
                        && int.TryParse(clause.Data?[0], out int dataValue)
                        && ((valToCheck > dataValue) || (clause.Invert && valToCheck <= dataValue)))
                {
                    return (true, !clause.Capture ? null : new TypedClauseCapture<int>(clause, valToCheck, state1, null));
                }
            }
            foreach (var val in stateTwoList)
            {
                if (int.TryParse(val, out int valToCheck)
                    && int.TryParse(clause.Data?[0], out int dataValue)
                    && ((valToCheck > dataValue) || (clause.Invert && valToCheck <= dataValue)))
                {
                    return (true, !clause.Capture ? null : new TypedClauseCapture<int>(clause, valToCheck, null, state2));
                }
            }
            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) LessThanOperation(Clause clause, object? state1, object? state2)
        {
            (var stateOneList, var stateOneDict) = ObjectToValues(state1);
            (var stateTwoList, var stateTwoDict) = ObjectToValues(state2);

            foreach (var val in stateOneList)
            {
                if (int.TryParse(val, out int valToCheck)
                        && int.TryParse(clause.Data?[0], out int dataValue)
                        && ((valToCheck < dataValue) || (clause.Invert && valToCheck >= dataValue)))
                {
                    return (true, !clause.Capture ? null : new TypedClauseCapture<int>(clause, valToCheck, state1, null));
                }
            }
            foreach (var val in stateTwoList)
            {
                if (int.TryParse(val, out int valToCheck)
                    && int.TryParse(clause.Data?[0], out int dataValue)
                    && ((valToCheck < dataValue) || (clause.Invert && valToCheck >= dataValue)))
                {
                    return (true, !clause.Capture ? null : new TypedClauseCapture<int>(clause, valToCheck, null, state2));
                }
            }
            return (false, null);
        }
        internal (bool Result, ClauseCapture? Capture) WasModifiedOperation(Clause clause, object? state1, object? state2)
        {
            var compareLogic = new CompareLogic();
            // Gather all differences if we are capturing
            compareLogic.Config.MaxDifferences = clause.Capture ? int.MaxValue : 1;

            var comparisonResult = compareLogic.Compare(state1, state2);
            if ((!comparisonResult.AreEqual && !clause.Invert) || (comparisonResult.AreEqual && clause.Invert))
            {
                return (true, !clause.Capture ? null : new TypedClauseCapture<ComparisonResult>(clause, comparisonResult, state1, state2));
            }
            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) IsNullOperation(Clause clause, object? state1, object? state2)
        {
            var res = state1 == null && state2 == null;
            res = clause.Invert ? !res : res;

            return (res, res && clause.Capture ? new ClauseCapture(clause, state1, state2) : null);
        }

        internal (bool Result, ClauseCapture? Capture) IsTrueOperation(Clause clause, object? state1, object? state2)
        {
            var typeHolder = state1 ?? state2;

            if (typeHolder is bool)
            {
                var res1 = (bool?)state1 ?? false;
                var res2 = (bool?)state2 ?? false;
                var res = clause.Invert ? !(res1 || res2) : res1 || res2;
                return (res, (!clause.Capture || !res) ? null : new TypedClauseCapture<bool>(clause, res1 || res2, state1, state2));
            }
            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) IsBeforeOperation(Clause clause, object? state1, object? state2)
        {
            var typeHolder = state1 ?? state2;

            if (typeHolder is DateTime)
            {
                foreach (var data in clause.Data ?? new List<string>())
                {
                    var compareTime = DateTime.TryParse(data, out DateTime result);

                    if (state1 is DateTime date1)
                    {
                        var res = date1.CompareTo(result) < 0;
                        if ((res && !clause.Invert) || (clause.Invert && !res))
                        {
                            return (true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date1, state1, null));
                        }
                    }
                    if (state2 is DateTime date2 && date2.CompareTo(result) < 0)
                    {
                        var res = date2.CompareTo(result) < 0;
                        if ((res && !clause.Invert) || (clause.Invert && !res))
                        {
                            return (true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date2, null, state2));
                        }
                    }
                }
            }

            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) IsExpiredOperation(Clause clause, object? state1, object? state2)
        {
            if (state1 is DateTime date1)
            {
                var res = date1.CompareTo(DateTime.Now) < 0;
                if ((res && !clause.Invert) || (clause.Invert && !res))
                    return (true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date1, state1, null));
            }
            if (state2 is DateTime date2)
            {
                var res = date2.CompareTo(DateTime.Now) < 0;
                if ((res && !clause.Invert) || (clause.Invert && !res))
                    return (true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date2, null, state2));
            }
            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) ContainsKeyOperation(Clause clause, object? state1, object? state2)
        {
            (var _, var stateOneDict) = ObjectToValues(state1);
            (var _, var stateTwoDict) = ObjectToValues(state2);

            var results = new List<string>();

            foreach (var datum in stateOneDict.ToList() ?? new List<KeyValuePair<string, string>>())
            {
                var res = clause.Data.Any(x => x == datum.Key);
                if ((res && !clause.Invert) || (clause.Invert && !res))
                {
                    results.Add(datum.Key);
                }
            }

            if (results.Any())
            {
                return (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, state1, null));
            }

            foreach (var datum in clause.Data ?? new List<string>())
            {
                var res = stateTwoDict.Any(x => x.Key == datum);
                if ((res && !clause.Invert) || (clause.Invert && !res))
                {
                    results.Add(datum);
                }
            }

            if (results.Any())
            {
                return (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, null, state2));

            }

            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) IsAfterOperation(Clause clause, object? state1, object? state2)
        {
            var typeHolder = state1 ?? state2;

            if (typeHolder is DateTime)
            {
                foreach (var data in clause.Data ?? new List<string>())
                {
                    var compareTime = DateTime.TryParse(data, out DateTime result);

                    if (state1 is DateTime date1)
                    {
                        var res = date1.CompareTo(result) > 0;
                        if ((res && !clause.Invert) || (clause.Invert && !res))
                            return (true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date1, state1, null));
                    }
                    if (state2 is DateTime date2)
                    {
                        var res = date2.CompareTo(result) > 0;
                        if ((res && !clause.Invert) || (clause.Invert && !res))
                            return (true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date2, null, state2));
                    }
                }
            }

            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) StartsWithOperation(Clause clause, object? state1, object? state2)
        {
            (var stateOneList, var stateOneDict) = ObjectToValues(state1);
            (var stateTwoList, var stateTwoDict) = ObjectToValues(state2);
            if (clause.Data is List<string> StartsWithData)
            {
                var results = new List<string>();
                foreach (var entry in stateOneList)
                {
                    var res = StartsWithData.Any(x => entry.StartsWith(x));
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        results.Add(entry);
                    }
                }

                if (results.Any())
                {
                    var typeHolder = state1 ?? state2;

                    return typeHolder switch
                    {
                        string _ => (true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, results.First(), state1, null)),
                        _ => (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, state1, null)),
                    };
                }

                foreach (var entry in stateTwoList)
                {
                    var res = StartsWithData.Any(x => entry.StartsWith(x));
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        results.Add(entry);
                    }
                }

                if (results.Any())
                {
                    var typeHolder = state1 ?? state2;

                    return typeHolder switch
                    {
                        string _ => (true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, results.First(), null, state2)),
                        _ => (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, null, state2)),
                    };
                }
            }
            return (false, null);
        }


        internal (bool Result, ClauseCapture? Capture) EndsWithOperation(Clause clause, object? state1, object? state2)
        {
            (var stateOneList, var stateOneDict) = ObjectToValues(state1);
            (var stateTwoList, var stateTwoDict) = ObjectToValues(state2);
            if (clause.Data is List<string> EndsWithData)
            {
                var results = new List<string>();
                foreach (var entry in stateOneList)
                {
                    var res = EndsWithData.Any(x => entry.EndsWith(x));
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        results.Add(entry);
                    }
                }

                if (results.Any())
                {
                    var typeHolder = state1 ?? state2;

                    return typeHolder switch
                    {
                        string _ => (true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, results.First(), state1, null)),
                        _ => (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, state1, null)),
                    };
                }

                foreach (var entry in stateTwoList)
                {
                    var res = EndsWithData.Any(x => entry.EndsWith(x));
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        results.Add(entry);
                    }
                }

                if (results.Any())
                {
                    var typeHolder = state1 ?? state2;

                    return typeHolder switch
                    {
                        string _ => (true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, results.First(), null, state2)),
                        _ => (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, null, state2)),
                    };
                }
            }
            return (false, null);
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public (bool Result, ClauseCapture? Capture) NopOperation(Clause clause, object? state1, object? state2)
        {
            Log.Debug($"{clause.Operation} is not supported.");
            return (false, null);
        }

        internal (bool Result, ClauseCapture? Capture) EqualsOperation(Clause clause, object? state1, object? state2)
        {
            (var stateOneList, _) = ObjectToValues(state1);
            (var stateTwoList, _) = ObjectToValues(state2);
            if (clause.Data is List<string> EqualsData)
            {
                List<string> StateListToEqList(List<string> stateList)
                {
                    var results = new List<string>();
                    foreach (var datum in EqualsData)
                    {
                        foreach (var stateOneDatum in stateList)
                        {
                            if (clause.Invert && stateOneDatum != datum)
                            {
                                results.Add(stateOneDatum);
                            }
                            else if (!clause.Invert && stateOneDatum == datum)
                            {
                                results.Add(stateOneDatum);
                            }
                        }
                    }
                    return results;
                }

                var res = StateListToEqList(stateOneList);
                if (res.Any())
                {
                    var typeHolder = state1 ?? state2;

                    return typeHolder switch
                    {
                        string _ => (true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, res.First(), state1, null)),
                        _ => (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, res, state1, null)),
                    };
                }
                res = StateListToEqList(stateTwoList);
                if (res.Any())
                {
                    var typeHolder = state1 ?? state2;

                    return typeHolder switch
                    {
                        string _ => (true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, res.First(), null, state2)),
                        _ => (true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, res, null, state2)),
                    };
                }
            }
            return (false, null);
        }

        /// <summary>
        /// Extracts string Values from an Object.  Will call the custom ObjectToValues delegate.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>A tuple of A list of Strings extracted and a List of KVP extracted.</returns>
        public (List<string>, List<KeyValuePair<string, string>>) ObjectToValues(object? obj)
        {
            var valsToCheck = new List<string>();
            // This supports both Dictionaries and Lists of KVP
            var dictToCheck = new List<KeyValuePair<string, string>>();
            if (obj != null)
            {
                try
                {
                    if (obj is List<string> stringList)
                    {
                        valsToCheck.AddRange(stringList);
                    }
                    else if (obj is Dictionary<string, string> dictString)
                    {
                        dictToCheck = dictString.ToList();
                    }
                    else if (obj is Dictionary<string, List<string>> dict)
                    {
                        dictToCheck = new List<KeyValuePair<string, string>>();
                        foreach (var list in dict.ToList())
                        {
                            foreach (var entry in list.Value)
                            {
                                dictToCheck.Add(new KeyValuePair<string, string>(list.Key, entry));
                            }
                        }
                    }
                    else if (obj is List<KeyValuePair<string, string>> listKvp)
                    {
                        dictToCheck = listKvp;
                    }
                    else
                    {
                        var found = false;
                        foreach (var del in CustomObjectToValuesDelegates)
                        {
                            var res = del?.Invoke(obj);
                            if (res.HasValue && res.Value.Processed)
                            {
                                found = true;
                                (valsToCheck, dictToCheck) = (res.Value.valsExtracted.ToList(), res.Value.dictExtracted.ToList());
                                break;
                            }
                        }
                        if (!found)
                        {
                            var val = obj?.ToString();
                            if (val is string)
                            {
                                valsToCheck.Add(val);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Log.Debug("Failed to Turn Obect into Values");
                }
            }

            return (valsToCheck, dictToCheck);
        }

        private static bool Operate(BOOL_OPERATOR Operator, bool first, bool second)
        {
            return Operator switch
            {
                BOOL_OPERATOR.AND => first && second,
                BOOL_OPERATOR.OR => first || second,
                BOOL_OPERATOR.XOR => first ^ second,
                BOOL_OPERATOR.NAND => !(first && second),
                BOOL_OPERATOR.NOR => !(first || second),
                BOOL_OPERATOR.NOT => !first,
                _ => false
            };
        }

        private (bool Success, List<ClauseCapture>? Capture) Evaluate(string[] splits, List<Clause> Clauses, object? state1, object? state2, IEnumerable<ClauseCapture>? captures = null)
        {
            bool current = false;

            var captureOut = new List<ClauseCapture>();

            var invertNextStatement = false;
            var operatorExpected = false;

            BOOL_OPERATOR Operator = BOOL_OPERATOR.OR;

            var updated_i = 0;

            for (int i = 0; i < splits.Length; i = updated_i)
            {
                if (operatorExpected)
                {
                    Operator = (BOOL_OPERATOR)Enum.Parse(typeof(BOOL_OPERATOR), splits[i]);
                    operatorExpected = false;
                    updated_i = i + 1;
                }
                else if (splits[i].StartsWith("("))
                {
                    //Get the substring closing this paren
                    var matchingParen = FindMatchingParen(splits, i);

                    // First remove the parenthesis from the beginning and end
                    splits[i] = splits[i][1..];
                    splits[matchingParen] = splits[matchingParen][0..^1];

                    bool EvaluateLambda()
                    {
                        // Recursively evaluate the contents of the parentheses
                        var capturesUnion = captures is null ? captureOut : captureOut.Union(captures);
                        var evaluation = Evaluate(splits[i..(matchingParen + 1)], Clauses, state1, state2, capturesUnion);

                        if (evaluation.Success)
                        {
                            captureOut.AddRange(evaluation.Capture ?? new List<ClauseCapture>());
                        }

                        var next = invertNextStatement ? !evaluation.Success : evaluation.Success;

                        return Operate(Operator, current, next);
                    }

                    // One of the labels ahead has a capture, so we can't shortcut
                    if (Clauses.Any(x => x.Capture && splits[i..(matchingParen + 1)].Contains(x.Label)))
                    {
                        current = EvaluateLambda();
                    }
                    else if (TryShortcut(current, Operator) is (bool CanShortcut, bool Value) && CanShortcut)
                    {
                        current = Value;
                    }
                    else
                    {
                        current = EvaluateLambda();
                    }

                    updated_i = matchingParen + 1;
                    invertNextStatement = false;
                    operatorExpected = true;
                }
                else
                {
                    if (splits[i].Equals(BOOL_OPERATOR.NOT.ToString()))
                    {
                        invertNextStatement = !invertNextStatement;
                        operatorExpected = false;
                    }
                    else
                    {
                        // Ensure we have exactly 1 matching clause defined
                        var targetLabel = splits[i].Replace("(", "").Replace(")", "");
                        var res = Clauses.Where(x => x.Label == targetLabel);
                        if (res.Count() > 1)
                        {
                            Log.Debug($"Multiple Clauses match the label {res.First().Label} so skipping evaluation of expression.  Run EnumerateRuleIssues to identify rule issues.");
                            return (false, null);
                        }
                        
                        // If we couldn't find a label match fall back to trying to parse this as an index into clauses
                        if (res.Count() == 0 && int.TryParse(targetLabel, out int result) && Clauses.Count > result)
                        {
                            res = new Clause[] { Clauses[result] };
                        }

                        var clause = res.First();

                        var shortcut = TryShortcut(current, Operator);

                        if (shortcut.CanShortcut && !Clauses.Any(x => x.Capture))
                        {
                            current = shortcut.Value;
                        }
                        else
                        {
                            var captureEnumerable = captures is null ? captureOut : captureOut.Union(captures);
                            var res2 = GetClauseCapture(res.First(), state1, state2, captureEnumerable);

                            if (res2.Applies && res2.Capture != null)
                            {
                                captureOut.Add(res2.Capture);
                            }

                            var next = invertNextStatement ? !res2.Applies : res2.Applies;

                            current = Operate(Operator, current, next);
                        }

                        invertNextStatement = false;
                        operatorExpected = true;
                    }
                    updated_i = i + 1;
                }
            }
            return (current, captureOut);
        }

        /// <summary>
        /// Try to shortcut a boolean operation
        /// </summary>
        /// <param name="current">The current boolean state</param>
        /// <param name="operation">The Operation</param>
        /// <returns>(If you can use a shortcut, the result of the shortcut)</returns>
        public static (bool CanShortcut, bool Value) TryShortcut(bool current, BOOL_OPERATOR operation)
        {
            // If either argument of an AND statement is false, or either argument of a
            // NOR statement is true, the result is always false and we can optimize
            // away evaluation of next
            if ((operation == BOOL_OPERATOR.AND && !current) ||
                (operation == BOOL_OPERATOR.NOR && current))
            {
                return (true, false);
            }
            // If either argument of an NAND statement is false, or either argument of
            // an OR statement is true, the result is always true and we can optimize
            // away evaluation of next
            if ((operation == BOOL_OPERATOR.OR && current) ||
                (operation == BOOL_OPERATOR.NAND && !current))
            {
                return (true, true);
            }
            return (false, false);
        }
    }
}
