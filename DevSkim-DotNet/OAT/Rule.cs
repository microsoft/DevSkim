// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace Microsoft.CST.OAT
{
    /// <summary>
    ///     A Rule holds Clauses and optionally has an Expression.
    /// </summary>
    public class Rule
    {
        /// <summary>
        /// The constructor for a Rule
        /// </summary>
        /// <param name="Name">The Name of the rule</param>
        public Rule(string Name) => this.Name = Name;

        /// <summary>
        ///     The list of Clauses to apply
        /// </summary>
        public List<Clause> Clauses { get; set; } = new List<Clause>();
        /// <summary>
        ///     A description of the rule
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        ///     The boolean expression to apply over the Clauses.
        ///
        ///     If Expression is null the Clauses will be ANDed together.
        /// </summary>
        public string? Expression { get; set; }
        /// <summary>
        ///     The name of the Rule
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///     The Name of the targeted object's Type
        /// </summary>
        public string? Target { get; set; }
        /// <summary>
        ///     An int associated with the rule
        /// </summary>
        public int Severity { get; set; }
        /// <summary>
        ///     A set of Tags assigned to the rule
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}