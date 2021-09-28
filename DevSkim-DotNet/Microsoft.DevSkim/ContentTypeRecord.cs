// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace DevSkim
{
    /// <summary>
    ///     Class to hold mapping from Visual Studio content type do DevSkim language
    /// </summary>
    internal class ContentTypeRecord
    {
        [JsonPropertyName("ds_types")]
        public string[]? DSTypes { get; set; }

        [JsonPropertyName("vs_type")]
        public string? VSType { get; set; }
    }
}