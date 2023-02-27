namespace DevSkim.LanguageServer
{
    using Microsoft.DevSkim;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    internal static class StaticScannerSettings
    {
        internal static ICollection<string> CustomRulePaths { get; set; } = Array.Empty<string>();
        internal static bool ScanOnOpen { get; set; }
        internal static bool ScanOnSave { get; set; }
        internal static bool ScanOnChange { get; set; }
        internal static bool RemoveFindingsOnClose { get; set; }
        internal static bool IgnoreDefaultRuleSet {get;set;}
        internal static DevSkimRuleSet RuleSet { get; set; } = new DevSkimRuleSet();
        internal static DevSkimRuleProcessorOptions RuleProcessorOptions { get; set; } = new DevSkimRuleProcessorOptions();
        internal static ICollection<string> IgnoreRuleIds { get; set; } = Array.Empty<string>();
        internal static ICollection<Regex> IgnoreFiles { get; set; } = Array.Empty<Regex>();
        internal static DevSkimRuleProcessor Processor { get; set; } = new DevSkimRuleProcessor(new DevSkimRuleSet(), new DevSkimRuleProcessorOptions());
    }
}
