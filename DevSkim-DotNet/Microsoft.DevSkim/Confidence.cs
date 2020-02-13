// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

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