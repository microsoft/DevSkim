using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.DevSkim.AI
{
    public class DevSkimRule : Rule
    {
        [JsonPropertyName("fix_its")]
        public List<CodeFix>? Fixes { get; set; }
    
        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; set; }
    
        [JsonPropertyName("confidence")]
        public Confidence Confidence { get; set; }
        
        [JsonPropertyName("rule_info")]
        public string? RuleInfo { get; set; }
    }
}