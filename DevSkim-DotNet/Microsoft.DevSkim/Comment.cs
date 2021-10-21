// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Comment class to hold information about comment for each language
    /// </summary>
    internal class Comment
    {
        [JsonPropertyName("inline")]
        public string? Inline { get; set; }

        [JsonPropertyName("language")]
        public string[]? Languages { get; set; }

        [JsonPropertyName("preffix")]
        public string? Prefix { get; set; }

        [JsonPropertyName("suffix")]
        public string? Suffix { get; set; }


        /// <summary>
        /// Set if the language should always be considered comments
        /// </summary>
        [JsonPropertyName("always")]
        public bool Always { get; set; }
    }
}