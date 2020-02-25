// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Class represents boundary in text
    /// </summary>
    public class Boundary
    {
        /// <summary>
        /// Starting position
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Length of boundary
        /// </summary>
        public int Length { get; set; }
    }
}
