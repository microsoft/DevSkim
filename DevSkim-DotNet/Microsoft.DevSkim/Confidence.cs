// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Issue severity
    /// </summary>
    [Flags]
    public enum Confidence 
    {        
        /// <summary>
        /// Issues that might exist
        /// </summary>
        Low = 1,
        /// <summary>
        /// Issues that likely exist
        /// </summary>
        Medium = 2,
        /// <summary>
        /// Issues that definitely exist
        /// </summary>
        High = 4,
    }
}