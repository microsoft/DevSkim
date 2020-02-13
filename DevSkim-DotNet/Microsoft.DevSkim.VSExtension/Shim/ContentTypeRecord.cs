// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// Class to hold mapping from Visual Studio content type do DevSkim language
    /// </summary>
    class ContentTypeRecord
    {
        [JsonProperty(PropertyName ="vs_type")]
        public string VSType { get; set; }
        [JsonProperty(PropertyName ="languages")]
        public string[] DSTypes { get; set; }
    }
}
