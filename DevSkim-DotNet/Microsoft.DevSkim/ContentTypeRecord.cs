// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace DevSkim
{
    /// <summary>
    /// Class to hold mapping from Visual Studio content type do DevSkim language
    /// </summary>
    class ContentTypeRecord
    {
        [JsonProperty(PropertyName ="vs_type")]
        public string? VSType { get; set; }
        [JsonProperty(PropertyName ="ds_types")]
        public string[]? DSTypes { get; set; }

    }
}
