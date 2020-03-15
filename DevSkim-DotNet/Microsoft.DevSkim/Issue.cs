// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.DevSkim
{

    /// <summary>
    /// Analysis Issue
    /// </summary>
    public class Issue
    {
        /// <summary>
        /// Boundary of issue (index, length)
        /// </summary>
        public Boundary? Boundary { get; set; }

        /// <summary>
        /// Location (line, column) where issue starts
        /// </summary>
        public Location? StartLocation { get; set; }

        /// <summary>
        /// Location (line, column) where issue ends
        /// </summary>
        public Location? EndLocation { get; set; }

        /// <summary>
        /// Matching rule
        /// </summary>
        public Rule? Rule { get; set; }

        /// <summary>
        /// True if Issue refers to suppression information 
        /// </summary>
        public bool IsSuppressionInfo { get; set; }
    }
}
