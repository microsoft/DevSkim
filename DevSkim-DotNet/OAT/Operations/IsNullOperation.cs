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
    /// The default IsNull operation
    /// </summary>
    public class IsNullOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public IsNullOperation(Analyzer analyzer) : base(Operation.IsNull, analyzer)
        {
            OperationDelegate = IsNullOperationDelegate;
            ValidationDelegate = IsNullValidationDelegate;
        }

        private IEnumerable<Violation> IsNullValidationDelegate(Rule rule, Clause clause)
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
        internal OperationResult IsNullOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            var res = state1 == null && state2 == null;
            res = clause.Invert ? !res : res;

            return new OperationResult(res, res && clause.Capture ? new ClauseCapture(clause, state1, state2) : null);
        }
    }
}
