// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.DevSkim
{
    [System.CLSCompliant(false)]
    public class ScopedRegexClause : Clause
    {
        public ScopedRegexClause(PatternScope scope, string? field = null) : base(Operation.Custom, field)
        {
            Scope = scope;
            CustomOperation = "ScopedRegex";
        }

        public PatternScope Scope { get; }
    }
}