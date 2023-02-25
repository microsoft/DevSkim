namespace DevSkim.LanguageServer
{
    using Microsoft.DevSkim;
    using System;
    using System.Collections.Generic;

    internal static class StaticScannerSettings
    {
        internal static bool ScanOnOpen { get; set; }
        internal static bool ScanOnSave { get; set; }
        internal static bool ScanOnChange { get; set; }
        internal static DevSkimRuleSet RuleSet { get; set; } = DevSkimRuleSet.GetDefaultRuleSet();
        internal static DevSkimRuleProcessorOptions RuleProcessorOptions { get; set; } = new DevSkimRuleProcessorOptions();
        internal static ICollection<string> IgnoreRuleIds { get; set; } = Array.Empty<string>();
        internal static ICollection<string> IgnoreFiles { get; set; } = Array.Empty<string>();
    }
}
