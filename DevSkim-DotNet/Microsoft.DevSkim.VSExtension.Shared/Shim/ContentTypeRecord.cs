// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    ///     Class to hold mapping from Visual Studio content type do DevSkim language
    /// </summary>
    internal class ContentTypeRecord
    {
        [JsonPropertyName("languages")]
        public string[] DSTypes { get; set; }

        [JsonPropertyName("vs_type")]
        public string VSType { get; set; }
    }
}