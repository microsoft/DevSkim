using Microsoft.CST.OAT.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.CST.OAT.Operations
{
    /// <summary>
    /// The default LessThan operation
    /// </summary>
    public class LessThanOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public LessThanOperation(Analyzer analyzer) : base(Operation.LessThan, analyzer)
        {
            OperationDelegate = LessThanOperationDelegate;
            ValidationDelegate = LessThanValidationDelegate;
        }

        private IEnumerable<Violation> LessThanValidationDelegate(Rule rule, Clause clause)
        {
            if (clause.Data?.Count == null || clause.Data is List<string> clauseList && (clauseList.Count != 1 || !int.TryParse(clause.Data.First(), out var _)))
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseExpectedInt"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            if (clause.DictData != null || clause.DictData?.Count > 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
            }
        }
        internal OperationResult LessThanOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            (var stateOneList, var stateOneDict) = Analyzer?.ObjectToValues(state1) ?? (new List<string>(), new List<KeyValuePair<string, string>>());
            (var stateTwoList, var stateTwoDict) = Analyzer?.ObjectToValues(state2) ?? (new List<string>(), new List<KeyValuePair<string, string>>());

            foreach (var val in stateOneList)
            {
                if (int.TryParse(val, out var valToCheck)
                        && int.TryParse(clause.Data?[0], out var dataValue)
                        && ((valToCheck < dataValue) || (clause.Invert && valToCheck >= dataValue)))
                {
                    return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<int>(clause, valToCheck, state1, null));
                }
            }
            foreach (var val in stateTwoList)
            {
                if (int.TryParse(val, out var valToCheck)
                    && int.TryParse(clause.Data?[0], out var dataValue)
                    && ((valToCheck < dataValue) || (clause.Invert && valToCheck >= dataValue)))
                {
                    return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<int>(clause, valToCheck, null, state2));
                }
            }
            return new OperationResult(false, null);
        }
    }
}
