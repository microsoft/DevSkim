// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.Security.DevSkim
{
    /// <summary>
    /// Class to hold search pattern
    /// </summary>
    public class SearchPattern
    {
        [JsonProperty(PropertyName = "applies_to")]
        public string[] AppliesTo { get; set; }

        [JsonProperty(PropertyName = "pattern")]
        public string Pattern { get; set; }

        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(PatternTypeConverter))]
        public PatternType Type { get; set; }

        [JsonProperty(PropertyName = "modifiers")]
        public string[] Modifiers { get; set; }
    }
}
