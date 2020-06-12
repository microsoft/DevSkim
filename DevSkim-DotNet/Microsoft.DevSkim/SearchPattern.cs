// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Class to hold search pattern
    /// </summary>
    public class SearchPattern
    {
        [JsonProperty(PropertyName = "modifiers")]
        public string[]? Modifiers { get; set; }

        [JsonProperty(PropertyName = "pattern")]
        public string? Pattern
        {
            get
            {
                return _pattern;
            }
            set
            {
                _compiled.Clear();
                _pattern = value;
            }
        }

        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(PatternTypeConverter))]
        public PatternType? PatternType { get; set; }

        [JsonProperty(PropertyName = "scopes")]
        public PatternScope[]? Scopes { get; set; }

        public Regex GetRegex(RegexOptions opts)
        {
            if (!_compiled.ContainsKey(opts))
            {
                _compiled[opts] = new Regex(Pattern, opts | RegexOptions.Compiled);
            }
            return _compiled[opts];
        }

        private Dictionary<RegexOptions, Regex> _compiled = new Dictionary<RegexOptions, Regex>();
        private string? _pattern;
    }
}