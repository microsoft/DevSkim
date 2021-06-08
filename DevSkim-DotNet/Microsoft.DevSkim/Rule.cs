// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Class to hold the Rule
    /// </summary>
    public class Rule
    {
        public Rule(string Id)
        {
            this.Id = Id;
        }

        [JsonProperty(PropertyName = "applies_to")]
        public List<string> AppliesTo { get; set; } = new List<string>();
        
        [JsonProperty(PropertyName = "does_not_apply_to")]
        public List<string> DoesNotApplyTo { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "conditions")]
        public List<SearchCondition> Conditions { get; set; } = new List<SearchCondition>();

        [JsonProperty(PropertyName = "confidence")]
        public Confidence Confidence { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string? Description { get; set; }

        /// <summary>
        ///     Runtime flag to disable the rule
        /// </summary>
        [JsonIgnore]
        public bool Disabled { get; set; }

        [JsonProperty(PropertyName = "fix_its")]
        public List<CodeFix> Fixes { get; set; } = new List<CodeFix>();

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "overrides")]
        public List<string> Overrides { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "patterns")]
        public List<SearchPattern> Patterns { get; set; } = new List<SearchPattern>();

        [JsonProperty(PropertyName = "recommendation")]
        public string? Recommendation { get; set; }

        [JsonProperty(PropertyName = "rule_info")]
        public string? RuleInfo { get; set; }

        /// <summary>
        ///     Optional tag assigned to the rule during runtime
        /// </summary>
        [JsonIgnore]
        public string? RuntimeTag { get; set; }

        [JsonProperty(PropertyName = "schema_version")]
        public int SchemaVersion { get; set; }

        [JsonProperty(PropertyName = "severity")]
        [JsonConverter(typeof(SeverityConverter))]
        public Severity Severity { get; set; }

        /// <summary>
        ///     Name of the source where the rule definition came from. Typically file, database or other storage.
        /// </summary>
        [JsonIgnore]
        public string? Source { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
}