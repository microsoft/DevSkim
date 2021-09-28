// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

        [JsonPropertyName("applies_to")]
        public List<string> AppliesTo { get; set; } = new List<string>();
        
        [JsonPropertyName("does_not_apply_to")]
        public List<string> DoesNotApplyTo { get; set; } = new List<string>();

        [JsonPropertyName("conditions")]
        public List<SearchCondition> Conditions { get; set; } = new List<SearchCondition>();

        [JsonPropertyName("confidence")]
        public Confidence Confidence { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        ///     Runtime flag to disable the rule
        /// </summary>
        [JsonIgnore]
        public bool Disabled { get; set; }

        [JsonPropertyName("fix_its")]
        public List<CodeFix> Fixes { get; set; } = new List<CodeFix>();

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("overrides")]
        public List<string> Overrides { get; set; } = new List<string>();

        [JsonPropertyName("patterns")]
        public List<SearchPattern> Patterns { get; set; } = new List<SearchPattern>();

        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; set; }

        [JsonPropertyName("rule_info")]
        public string? RuleInfo { get; set; }

        /// <summary>
        ///     Optional tag assigned to the rule during runtime
        /// </summary>
        [JsonIgnore]
        public string? RuntimeTag { get; set; }

        [JsonPropertyName("schema_version")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("severity")]
        [JsonConverter(typeof(SeverityConverter))]
        public Severity Severity { get; set; }

        /// <summary>
        ///     Name of the source where the rule definition came from. Typically file, database or other storage.
        /// </summary>
        [JsonIgnore]
        public string? Source { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
}