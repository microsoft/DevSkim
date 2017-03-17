// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.DevSkim
{

    /// <summary>
    /// Analysis Issue
    /// </summary>
    public class Issue
    {
        public Issue()
        {            
            Index = -1;
            Length = 0;
            Rule = null;
        }
        
        /// <summary>
        /// Start index of the match in the string
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Length of the match
        /// </summary>
        public int Length { get; set; }        

        /// <summary>
        /// Matching rule
        /// </summary>
        public Rule Rule { get; set; }
    }
}
