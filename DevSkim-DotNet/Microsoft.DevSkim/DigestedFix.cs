// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.DevSkim
{
    public class DigestedFix
    {
        string Name { get; set; } = string.Empty;
        string Replacement { get; set; } = string.Empty;
        /// <summary>
        ///     Location (line, column) where text to be replaced starts
        /// </summary>
        public Location? StartLocation { get; set; }
        /// <summary>
        ///     Location (line, column) where text to be replaced ends
        /// </summary>
        public Location? EndLocation { get; set; }
    }
}