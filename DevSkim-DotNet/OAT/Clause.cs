// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License.
using System.Collections.Generic;

namespace Microsoft.CST.OAT
{
    /// <summary>
    ///     Clauses contain an Operation and associated data
    /// </summary>
    public class Clause
    {
        /// <summary>
        /// Create a Clause
        /// </summary>
        /// <param name="operation">The Operation to Perform</param>
        /// <param name="field">Optionally, the path to the field to operate on</param>
        public Clause(Operation operation, string? field = null)
        {
            Operation = operation;
            Field = field;
        }

        /// <summary>
        ///     A list of strings passed to the operation
        /// </summary>
        public List<string>? Data { get; set; }
        /// <summary>
        ///     A dictionary of strings passed to the operation
        /// </summary>
        public List<KeyValuePair<string, string>>? DictData { get; set; }
        /// <summary>
        ///     Which field or property of the Target should this Clause apply to?
        ///
        ///     null is wildcard
        /// </summary>
        public string? Field { get; set; }
        /// <summary>
        ///     The Label used for the boolean Expression in the Rule containing this Clause
        ///
        ///     May be null iff Expression is null
        /// </summary>
        public string? Label { get; set; }
        /// <summary>
        ///     The Operation to perform
        /// </summary>
        public Operation Operation { get; set; }
        /// <summary>
        ///     A string indicating what custom operation should be performed, if Operation is CUSTOM
        /// </summary>
        public string? CustomOperation { get; set; }

        /// <summary>
        /// If the result of the Operation should be inverted
        /// </summary>
        public bool Invert { get; set; }

        /// <summary>
        /// When calling Capture functionality if the result of this clause should be Captured
        /// </summary>
        public bool Capture { get; set; }
    }
}