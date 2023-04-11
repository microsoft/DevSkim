namespace DevSkim.LanguageServer
{
    using Microsoft.DevSkim;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    internal static class StaticScannerSettings
    {
        internal static SuppressionStyle SuppressionStyle { get; set; } = SuppressionStyle.Line;
        internal static ICollection<string> CustomRulePaths { get; set; } = Array.Empty<string>();
        internal static ICollection<string> IgnoreRuleIds { get; set; } = Array.Empty<string>();
        internal static ICollection<Regex> IgnoreFiles { get; set; } = Array.Empty<Regex>();
        // Used to populate suppressions
        internal static string ReviewerName { get; set; } = string.Empty;
        // Suppression duration in days
        internal static int SuppressionDuration { get; set; } = 30;
        internal static bool IgnoreDefaultRuleSet { get; set; } = false;
        internal static bool ScanOnOpen { get; set; } = true;
        internal static bool ScanOnSave { get; set; } = true;
        internal static bool ScanOnChange { get; set; } = true;
        internal static bool RemoveFindingsOnClose { get; set; } = true;
        internal static DevSkimRuleSet RuleSet { get; set; } = new DevSkimRuleSet();
        internal static DevSkimRuleProcessorOptions RuleProcessorOptions { get; set; } = new DevSkimRuleProcessorOptions();
        internal static DevSkimRuleProcessor Processor { get; set; } = new DevSkimRuleProcessor(DevSkimRuleSet.GetDefaultRuleSet(), new DevSkimRuleProcessorOptions());
    }
}
