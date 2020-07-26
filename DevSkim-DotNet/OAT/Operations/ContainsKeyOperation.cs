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
    /// The default ContainsKey operation
    /// </summary>
    public class ContainsKeyOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public ContainsKeyOperation(Analyzer analyzer) : base(Operation.ContainsKey, analyzer)
        {
            OperationDelegate = ContainsKeyOperationDelegate;
            ValidationDelegate = ContainsKeyValidationDelegate;
        }

        private IEnumerable<Violation> ContainsKeyValidationDelegate(Rule rule, Clause clause)
        {
            if (clause.DictData != null)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseUnexpectedDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            if (clause.Data == null || clause.Data?.Count == 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseMissingListData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
        }
        internal OperationResult ContainsKeyOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            (var _, var stateOneDict) = Analyzer?.ObjectToValues(state1) ?? (new List<string>(), new List<KeyValuePair<string, string>>());
            (var _, var stateTwoDict) = Analyzer?.ObjectToValues(state2) ?? (new List<string>(), new List<KeyValuePair<string, string>>());

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
                return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, state1, null));
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
                return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<List<string>>(clause, results, null, state2));

            }

            return new OperationResult(false, null);
        }

    }
}
