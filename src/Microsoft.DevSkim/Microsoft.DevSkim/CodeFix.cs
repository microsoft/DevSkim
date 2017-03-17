// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Code fix class
    /// </summary>
    public class CodeFix
    {
        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(FixTypeConverter))]
        public FixType FixType { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "search")]
        public string Search { get; set; }

        [JsonProperty(PropertyName = "replace")]
        public string Replace { get; set; }
    }
}
