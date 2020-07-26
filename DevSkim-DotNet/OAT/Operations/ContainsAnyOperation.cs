using Microsoft.CST.OAT.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.CST.OAT.Operations
{
    /// <summary>
    /// The default ContainsAny operation
    /// </summary>
    public class ContainsAnyOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public ContainsAnyOperation(Analyzer analyzer) : base(Operation.ContainsAny, analyzer)
        {
            OperationDelegate = ContainsAnyOperationDelegate;
            ValidationDelegate = ContainsAnyValidationDelegate;
        }

        private IEnumerable<Violation> ContainsAnyValidationDelegate(Rule rule, Clause clause)
        {
            if ((clause.Data?.Count == null || clause.Data?.Count == 0) && (clause.DictData?.Count == null || clause.DictData?.Count == 0))
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoDataOrDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            if ((clause.Data is List<string> list && list.Count > 0) && (clause.DictData is List<KeyValuePair<string, string>> dictList && dictList.Count > 0))
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseBothDataDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
        }
        internal OperationResult ContainsAnyOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            var typeHolder = state1 ?? state2;

            if (typeHolder?.GetType().IsDefined(typeof(FlagsAttribute), false) is true)
            {
                bool ParseContainsAnyEnum(Enum state)
                {
                    foreach (var datum in clause.Data ?? new List<string>())
                    {
#if !NETSTANDARD2_0
                        if (Enum.TryParse(typeHolder.GetType(), datum, out var result))
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
                        return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<Enum>(clause, enum1, state1, null));
                    }
                }
                if (state2 is Enum enum2)
                {
                    var res = ParseContainsAnyEnum(enum2);
                    if ((res && !clause.Invert) || (clause.Invert && !res))
                    {
                        return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<Enum>(clause, enum2, null, state2));
                    }
                }

                return new OperationResult(false, null);
            }

            (var stateOneList, var stateOneDict) = Analyzer?.ObjectToValues(state1) ?? (new List<string>(), new List<KeyValuePair<string, string>>());
            (var stateTwoList, var stateTwoDict) = Analyzer?.ObjectToValues(state2) ?? (new List<string>(), new List<KeyValuePair<string, string>>());

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
                        return new OperationResult(true, returnVal);
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
                        return new OperationResult(true, returnVal);
                    }
                }
                return new OperationResult(false, null);
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
                            string _ => new OperationResult(true, new TypedClauseCapture<string>(clause, result.Matches.First(), state1, null)),
                            _ => new OperationResult(true, new TypedClauseCapture<List<string>>(clause, result.Matches, state1, null)),
                        };
                    }
                    else
                    {
                        return new OperationResult(true, null);
                    }
                }
                result = ClauseAppliesToList(stateTwoList);
                if (result.Applies)
                {
                    if (result.Matches?.Any() is true)
                    {
                        return typeHolder switch
                        {
                            string _ => new OperationResult(true, new TypedClauseCapture<string>(clause, result.Matches.First(), null, state2)),
                            _ => new OperationResult(true, new TypedClauseCapture<List<string>>(clause, result.Matches, null, state2)),
                        };
                    }
                    else
                    {
                        return new OperationResult(true, null);
                    }
                }
            }

            return new OperationResult(false, null);
        }

    }
}
