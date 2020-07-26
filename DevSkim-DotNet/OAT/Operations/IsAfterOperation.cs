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
    /// The default IsAfter operation
    /// </summary>
    public class IsAfterOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public IsAfterOperation(Analyzer analyzer) : base(Operation.IsAfter, analyzer)
        {
            OperationDelegate = IsAfterOperationDelegate;
            ValidationDelegate = IsAfterValidationDelegate;
        }

        private IEnumerable<Violation> IsAfterValidationDelegate(Rule rule, Clause clause)
        {
            if (clause.Data?.Count == null || clause.Data is List<string> clauseList2 && (clauseList2.Count != 1 || !DateTime.TryParse(clause.Data.First(), out DateTime _)))
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseExpectedDateTime"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            if (clause.DictData != null || clause.DictData?.Count > 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
            }
        }
        internal OperationResult IsAfterOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
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
                            return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date1, state1, null));
                    }
                    if (state2 is DateTime date2)
                    {
                        var res = date2.CompareTo(result) > 0;
                        if ((res && !clause.Invert) || (clause.Invert && !res))
                            return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<DateTime>(clause, date2, null, state2));
                    }
                }
            }

            return new OperationResult(false, null);
        }
    }
}
