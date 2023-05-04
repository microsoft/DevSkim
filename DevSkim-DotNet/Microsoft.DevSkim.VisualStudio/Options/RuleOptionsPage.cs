namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    [Guid(PageGuidString)]
    public class RuleOptionsPage : DialogPage
    {
        public const string PageGuidString = "62a64958-3e57-382c-b128-3b8aac89f463";

        [Category("Rule Options")]
        [DisplayName("Enable Critical Severity Rules")]
        [Description("Turn on the rules with severity \"Critical\".")]
        public bool EnableCriticalSeverityRules
        {
            get; set;
        } = true;

        [Category("Rule Options")]
        [DisplayName("Enable Important Severity Rules")]
        [Description("Turn on the rules with severity \"Important\".")]
        public bool EnableImportantSeverityRules
        {
            get; set;
        } = true;

        [Category("Rule Options")]
        [DisplayName("Enable Moderate Severity Rules")]
        [Description("Turn on the rules with severity \"Moderate\".")]
        public bool EnableModerateSeverityRules
        {
            get; set;
        } = true;

        [Category("Rule Options")]
        [DisplayName("Enable Manual Review Severity Rules")]
        [Description("Turn on the rules that flag things for manual review. " +
            "These are typically scenarios that *could* be incredibly severe if tainted data can be inserted, " +
            "but are often programmatically necessary (for example, dynamic code generation with \"eval\").  " +
            "Since these rules tend to require further analysis upon flagging an issue, they are disabled by default.")]
        public bool EnableManualReviewSeverityRules
        {
            get; set;
        } = false;

        [Category("Rule Options")]
        [DisplayName("Enable Best Practice Severity Rules")]
        [Description("Turn on the rules with severity \"Best-Practice\". " +
            "These rules either flag issues that are typically of a lower severity, " +
            "or recommended practices that lead to more secure code, but aren't typically outright vulnerabilities.")]
        public bool EnableBestPracticeSeverityRules
        {
            get; set;
        } = false;

        [Category("Rule Options")]
        [DisplayName("Enable High Confidence Rules")]
        [Description("Turn on the rules of confidence \"High\".")]
        public bool EnableHighConfidenceRules
        {
            get; set;
        } = true;

        [Category("Rule Options")]
        [DisplayName("Enable Medium Confidence Rules")]
        [Description("Turn on the rules of confidence \"Medium\".")]
        public bool EnableMediumConfidenceRules
        {
            get; set;
        } = true;

        [Category("Rule Options")]
        [DisplayName("Enable Low Confidence Rules")]
        [Description("Turn on the rules of confidence \"Low\".")]
        public bool EnableLowConfidenceRules
        {
            get; set;
        } = false;

        [Category("Rule Options")]
        [DisplayName("Custom Rules Paths")]
        [Description("A list of local paths on disk to rules files or folders containing rule files, " +
            "for DevSkim to use in analysis.")]
        public List<string> CustomRulesPaths
        {
            get; set;
        } = new List<string>();

        [Category("Rule Options")]
        [DisplayName("Custom Languages Path")]
        [Description("A local path to a custom language file for analysis. Also requires customCommentsPath to be set.")]
        public string CustomLanguagesPath
        {
            get; set;
        } = "";

        [Category("Rule Options")]
        [DisplayName("Custom Comments Path")]
        [Description("A local path to a custom comments file for analysis. Also requires customLanguagesPath to be set.")]
        public string CustomCommentsPath
        {
            get; set;
        } = "";
    }
}
