using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim.LanguageProtoInterop
{
    public class PortableScannerSettings
    {
        public string SuppressionStyle { get; set; } = "line";
        public ICollection<string> CustomRulePaths { get; set; } = Array.Empty<string>();
        public  ICollection<string> IgnoreRuleIds { get; set; } = Array.Empty<string>();
        public  ICollection<Regex> IgnoreFiles { get; set; } = Array.Empty<Regex>();
        // Used to populate suppressions
        public  string ReviewerName { get; set; } = string.Empty;
        // Suppression duration in days
        public  int SuppressionDuration { get; set; } = 30;
        public  bool IgnoreDefaultRuleSet { get; set; } = false;
        public  bool ScanOnOpen { get; set; } = true;
        public  bool ScanOnSave { get; set; } = true;
        public  bool ScanOnChange { get; set; } = true;
        public  bool RemoveFindingsOnClose { get; set; } = true;
        public  string CustomLanguagesPath { get; set; } = string.Empty;
        public  string CustomCommentsPath { get; set; } = string.Empty;
    }
}