// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Class to hold search pattern
    /// </summary>
    public class SearchPattern
    {
        [JsonPropertyName("modifiers")]
        public string[]? Modifiers { get; set; }

        [JsonPropertyName("pattern")]
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

        [JsonPropertyName("type")]
        [JsonConverter(typeof(PatternTypeConverter))]
        public PatternType? PatternType { get; set; }

        [JsonPropertyName("scopes")]
        public PatternScope[]? Scopes { get; set; }

        public Regex GetRegex(RegexOptions opts)
        {
            if (!_compiled.ContainsKey(opts))
            {
                _compiled[opts] = new Regex(Pattern ?? string.Empty, opts | RegexOptions.Compiled);
            }
            return _compiled[opts];
        }

        private Dictionary<RegexOptions, Regex> _compiled = new Dictionary<RegexOptions, Regex>();
        private string? _pattern;
    }
}