// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Class to hold search pattern
    /// </summary>
    public class SearchPattern
    {
        [JsonProperty(PropertyName = "pattern")]
        public string Pattern { get; set; }

        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(PatternTypeConverter))]
        public PatternType PatternType { get; set; }

        [JsonProperty(PropertyName = "modifiers")]
        public string[] Modifiers { get; set; }

        [JsonProperty(PropertyName = "scopes")]        
        public PatternScope[] Scopes { get; set; }
    }
}
