// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Content Type class
    /// </summary>
    internal class LanguageInfo
    {
        [JsonPropertyName("extensions")]
        public string[]? Extensions { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}