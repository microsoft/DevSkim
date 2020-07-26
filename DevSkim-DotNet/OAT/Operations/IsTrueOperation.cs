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
    /// The default IsTrue operation
    /// </summary>
    public class IsTrueOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public IsTrueOperation(Analyzer analyzer) : base(Operation.IsTrue, analyzer)
        {
            OperationDelegate = IsTrueOperationDelegate;
            ValidationDelegate = IsTrueValidationDelegate;
        }

        private IEnumerable<Violation> IsTrueValidationDelegate(Rule rule, Clause clause)
        {
            if (!(clause.Data?.Count == null || clause.Data?.Count == 0))
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseRedundantData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            else if (!(clause.DictData?.Count == null || clause.DictData?.Count == 0))
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseRedundantDictData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
        }
        internal OperationResult IsTrueOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            var typeHolder = state1 ?? state2;

            if (typeHolder is bool)
            {
                var res1 = (bool?)state1 ?? false;
                var res2 = (bool?)state2 ?? false;
                var res = clause.Invert ? !(res1 || res2) : res1 || res2;
                return new OperationResult(res, (!clause.Capture || !res) ? null : new TypedClauseCapture<bool>(clause, res1 || res2, state1, state2));
            }
            return new OperationResult(false, null);
        }
    }
}
