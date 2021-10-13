﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;
using System;

namespace Microsoft.DevSkim
{
    internal class WithinClause : Clause
    {
        public WithinClause(string? field = null) : base(Operation.Custom, field)
        {
            CustomOperation = "Within";
        }

        public int After { get; set; }
        public int Before { get; set; }
        public bool OnlyBefore { get; set; }
        public bool OnlyAfter { get; set; }
        public bool SameFile { get; set; }
        public bool FindingOnly { get; set; }
        public bool SameLineOnly { get; internal set; }
        public PatternScope[] Scopes { get; } = Array.Empty<PatternScope>();
    }
}