using Microsoft.CST.OAT.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CST.OAT.Operations
{
    /// <summary>
    /// This delegate allows extending the Analyzer with a custom operation.
    /// </summary>
    /// <param name="clause">The clause being applied</param>
    /// <param name="state1">The first object state</param>
    /// <param name="state2">The second object state</param>
    /// <param name="captures">The previously found clause captures</param>
    /// <returns>(If the Operation delegate applies to the clause, If the operation was successful, if capturing is enabled the ClauseCapture)</returns>
    public delegate OperationResult OperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures);

    /// <summary>
    /// This delegate allows extending the Analyzer with extra rule validation for custom rules.
    /// </summary>
    /// <param name="rule">The Target Rule</param>
    /// <param name="clause">The Target Clause</param>
    /// <returns>(If the validation applied, The Enumerable of Violations found)</returns>
    public delegate IEnumerable<Violation> ValidationDelegate(Rule rule, Clause clause);
    /// <summary>
    /// A Class Representing a complete implemented operation including its Operation and Validation delegates
    /// </summary>
    public class OatOperation
    {
        /// <summary>
        /// The fully specified constructor
        /// </summary>
        /// <param name="operation">The Operation this implements</param>
        /// <param name="operationDelegate">The Operation Delegate</param>
        /// <param name="validationDelegate">The Validation Delegate</param>
        /// <param name="analyzer">The Analyzer Context</param>
        /// <param name="customOperation">The CustomOperation to use if operation is Custom</param>
        public OatOperation(Operation operation, OperationDelegate operationDelegate, ValidationDelegate validationDelegate, Analyzer analyzer, string? customOperation = null) : this(operation,analyzer)
        {
            OperationDelegate = operationDelegate;
            ValidationDelegate = validationDelegate;
            CustomOperation = customOperation;
        }

        /// <summary>
        /// Simple constructor
        /// </summary>
        /// <param name="operation">The operation this implements</param>
        /// <param name="analyzer">The analyzer context</param>
        public OatOperation(Operation operation, Analyzer analyzer) : this(operation)
        {
            Analyzer = analyzer;
        }

        /// <summary>
        /// Simplest constructor without analyzer context
        /// </summary>
        /// <param name="operation">The Operation this implements</param>
        public OatOperation(Operation operation)
        {
            Operation = operation;
        }

        /// <summary>
        /// The analyzer context
        /// </summary>
        public Analyzer? Analyzer { get; set; }
        /// <summary>
        /// The validation delegate
        /// </summary>
        public ValidationDelegate ValidationDelegate { get; set; } = UndefinedValidation;
        /// <summary>
        /// The Operation implemented
        /// </summary>
        public Operation Operation { get; set; } = Operation.Custom;
        /// <summary>
        /// The operation delegate
        /// </summary>
        public OperationDelegate OperationDelegate { get; set; } = NopOperation;
        /// <summary>
        /// The CustomOperation name if Operation is Custom
        /// </summary>
        public string? CustomOperation { get; set; }

        internal string Key
        {
            get
            {
                if (string.IsNullOrEmpty(_key))
                {
                    _key = string.Format("{0}{1}{2}", Operation, CustomOperation is null ? "" : " - ", CustomOperation is null ? "" : CustomOperation);
                }
                return _key;
            }
        }

        /// <summary>
        /// Yields one violation.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="clause"></param>
        public static IEnumerable<Violation> UndefinedValidation(Rule rule, Clause clause)
        {
            yield return new Violation(string.Format(Strings.Get("Err_ValidationDelegateUndefined_{0}{1}{2}"), rule.Name, clause.Label, clause.CustomOperation), rule, clause);
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <param name="captures"></param>
        /// <returns></returns>
        public static OperationResult NopOperation(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            Log.Debug($"{clause.Operation} is not supported.");
            return new OperationResult(false, null);
        }
        private string _key = "";
    }
}
