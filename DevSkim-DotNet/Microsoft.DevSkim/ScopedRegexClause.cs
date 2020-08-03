// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;

namespace Microsoft.DevSkim
{
    public class ScopedRegexClause : Clause
    {
        public ScopedRegexClause(PatternScope[] scopes, string? field = null) : base(Operation.Custom, field)
        {
            Scopes = scopes;
            CustomOperation = "ScopedRegex";
        }

        public PatternScope[] Scopes { get; }
    }
}