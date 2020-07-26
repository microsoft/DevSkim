using Microsoft.CST.OAT.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.CST.OAT.Operations
{
    /// <summary>
    /// The default StartsWith operation
    /// </summary>
    public class StartsWithOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public StartsWithOperation(Analyzer analyzer) : base(Operation.StartsWith, analyzer)
        {
            OperationDelegate = StartsWithOperationDelegate;
            ValidationDelegate = StartsWithValidationDelegate;
        }

        private IEnumerable<Violation> StartsWithValidationDelegate(Rule rule, Clause clause)
        {
            if (clause.Data?.Count == null || clause.Data?.Count == 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            if (clause.DictData != null || clause.DictData?.Count > 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
            }
        }
        internal OperationResult StartsWithOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            (var stateOneList, var stateOneDict) = Analyzer?.ObjectToValues(state1) ?? (new List<string>(), new List<KeyValuePair<string, string>>());
            (var stateTwoList, var stateTwoDict) = Analyzer?.ObjectToValues(state2) ?? (new List<string>(), new List<KeyValuePair<string, string>>());
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
                        string _ => new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, results.First(), state1, null)),
                        _ => new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, state1, null)),
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
                        string _ => new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<string>(clause, results.First(), null, state2)),
                        _ => new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, null, state2)),
                    };
                }
            }
            return new OperationResult(false, null);
        }
    }
}
