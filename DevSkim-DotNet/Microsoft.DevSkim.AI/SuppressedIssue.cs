// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.DevSkim.AI
{
    /// <summary>
    ///     Class holds information about suppressed issue
    /// </summary>
    public class SuppressedIssue
    {
        public Boundary? Boundary { get; set; }
        public string? ID { get; set; }
    }
}