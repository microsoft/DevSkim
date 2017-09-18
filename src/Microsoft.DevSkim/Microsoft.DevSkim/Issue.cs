// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.DevSkim
{

    /// <summary>
    /// Analysis Issue
    /// </summary>
    public class Issue
    {
        /// <summary>
        /// Creates new instance of Issue
        /// </summary>
        public Issue()
        {                        
            Rule = null;
            Boundary = new Boundary();
        }
        
        public Boundary Boundary { get; set; }
        public Location Location { get; set; }

        /// <summary>
        /// Matching rule
        /// </summary>
        public Rule Rule { get; set; }
    }
}
