// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;
using System;
using System.Text.Json;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Code Fix Type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FixType
    {
        RegexReplace
    }
}