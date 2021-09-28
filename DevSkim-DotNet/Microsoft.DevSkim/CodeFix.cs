﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Code fix class
    /// </summary>
    public class CodeFix
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(FixTypeConverter))]
        public FixType? FixType { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("pattern")]
        public SearchPattern? Pattern { get; set; }

        [JsonPropertyName("replacement")]
        public string? Replacement { get; set; }
    }
}