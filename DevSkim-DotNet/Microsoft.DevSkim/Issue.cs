// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.DevSkim
{
    using System.Collections.Generic;

    /// <summary>
    ///     Analysis Issue
    /// </summary>
    public class Issue
    {
        public Issue(Boundary Boundary, Location StartLocation, Location EndLocation, Rule Rule, System.Collections.Generic.List<string>? Fixes)
        {
            this.Boundary = Boundary;
            this.StartLocation = StartLocation;
            this.EndLocation = EndLocation;
            this.Rule = Rule;
            this.Fixes = Fixes;
        }

        /// <summary>
        ///     Boundary of issue (index, length)
        /// </summary>
        public Boundary Boundary { get; set; }

        /// <summary>
        ///     Location (line, column) where issue ends
        /// </summary>
        public Location EndLocation { get; set; }

        /// <summary>
        ///     True if Issue refers to suppression information
        /// </summary>
        public bool IsSuppressionInfo { get; set; }

        /// <summary>
        ///     Matching rule
        /// </summary>
        public Rule Rule { get; set; }

        /// <summary>
        ///     Processed fixes which can directly replace the provided boundary
        /// </summary>
        public List<string>? Fixes { get; }

        /// <summary>
        ///     Location (line, column) where issue starts
        /// </summary>
        public Location StartLocation { get; set; }
    }
}