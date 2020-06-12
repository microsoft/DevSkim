// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Content Type class
    /// </summary>
    internal class LanguageInfo
    {
        [JsonProperty(PropertyName = "extensions")]
        public string[]? Extensions { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }
    }
}