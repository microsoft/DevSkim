// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    ///     Class to hold mapping from Visual Studio content type do DevSkim language
    /// </summary>
    internal class ContentTypeRecord
    {
        [JsonProperty(PropertyName = "languages")]
        public string[] DSTypes { get; set; }

        [JsonProperty(PropertyName = "vs_type")]
        public string VSType { get; set; }
    }
}