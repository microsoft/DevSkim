namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.InteropServices;

    [Guid(PageGuidString)]
    public class GeneralOptionsPage : DialogPage
    {
        public const string PageGuidString = "c88696f6-dd46-380e-a706-14e73fd51564";
        private const string RulesCategory = "Rules";
        private const string SuppressionsCategory = "Suppressions";
        private const string GuidanceCategory = "Guidance";
        private const string IgnoresCategory = "Ignores";
        private const string FindingsCategory = "Findings";
        private const string TriggersCategory = "Triggers";

        /// <summary>
        /// Rule Options
        /// </summary>
        [Category(RulesCategory)]
        [DisplayName("Enable Critical Severity Rules")]
        [Description("Turn on the rules with severity \"Critical\".")]
        public bool EnableCriticalSeverityRules
        {
            get => StaticSettings.portableSettings.EnableCriticalSeverity;
            set
            {
                StaticSettings.portableSettings.EnableCriticalSeverity = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Enable Important Severity Rules")]
        [Description("Turn on the rules with severity \"Important\".")]
        public bool EnableImportantSeverityRules
        {
            get => StaticSettings.portableSettings.EnableImportantSeverityRules;
            set
            {
                StaticSettings.portableSettings.EnableImportantSeverityRules = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Enable Moderate Severity Rules")]
        [Description("Turn on the rules with severity \"Moderate\".")]
        public bool EnableModerateSeverityRules
        {
            get => StaticSettings.portableSettings.EnableModerateSeverityRules;
            set
            {
                StaticSettings.portableSettings.EnableModerateSeverityRules = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Enable Manual Review Severity Rules")]
        [Description("Turn on the rules that flag things for manual review. " +
            "These are typically scenarios that *could* be incredibly severe if tainted data can be inserted, " +
            "but are often programmatically necessary (for example, dynamic code generation with \"eval\").  " +
            "Since these rules tend to require further analysis upon flagging an issue, they are disabled by default.")]
        public bool EnableManualReviewSeverityRules
        {
            get => StaticSettings.portableSettings.EnableManualReviewSeverityRules;
            set
            {
                StaticSettings.portableSettings.EnableManualReviewSeverityRules = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Enable Best Practice Severity Rules")]
        [Description("Turn on the rules with severity \"Best-Practice\". " +
            "These rules either flag issues that are typically of a lower severity, " +
            "or recommended practices that lead to more secure code, but aren't typically outright vulnerabilities.")]
        public bool EnableBestPracticeSeverityRules
        {
            get => StaticSettings.portableSettings.EnableBestPracticeSeverityRules;
            set
            {
                StaticSettings.portableSettings.EnableBestPracticeSeverityRules = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Enable High Confidence Rules")]
        [Description("Turn on the rules of confidence \"High\".")]
        public bool EnableHighConfidenceRules
        {
            get => StaticSettings.portableSettings.EnableHighConfidenceRules;
            set
            {
                StaticSettings.portableSettings.EnableHighConfidenceRules = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Enable Medium Confidence Rules")]
        [Description("Turn on the rules of confidence \"Medium\".")]
        public bool EnableMediumConfidenceRules
        {
            get => StaticSettings.portableSettings.EnableMediumConfidenceRules;
            set
            {
                StaticSettings.portableSettings.EnableMediumConfidenceRules = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Enable Low Confidence Rules")]
        [Description("Turn on the rules of confidence \"Low\".")]
        public bool EnableLowConfidenceRules
        {
            get => StaticSettings.portableSettings.EnableLowConfidenceRules;
            set
            {
                StaticSettings.portableSettings.EnableLowConfidenceRules = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Custom Rules Paths")]
        [Description("A list of local paths on disk to rules files or folders containing rule files, " +
            "for DevSkim to use in analysis.")]
        public List<string> CustomRulesPaths
        {
            get => StaticSettings.portableSettings.CustomRulePaths.ToList();
            set
            {
                StaticSettings.portableSettings.CustomRulePaths = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Custom Languages Path")]
        [Description("A local path to a custom language file for analysis. Also requires customCommentsPath to be set.")]
        public string CustomLanguagesPath
        {
            get => StaticSettings.portableSettings.CustomLanguagesPath;
            set
            {
                StaticSettings.portableSettings.CustomLanguagesPath = value;
            }
        }

        [Category(RulesCategory)]
        [DisplayName("Custom Comments Path")]
        [Description("A local path to a custom comments file for analysis. Also requires customLanguagesPath to be set.")]
        public string CustomCommentsPath
        {
            get => StaticSettings.portableSettings.CustomCommentsPath;
            set
            {
                StaticSettings.portableSettings.CustomCommentsPath = value;
            }
        }


        /// <summary>
        /// Suppression Options
        /// </summary>
        [Category(SuppressionsCategory)]
        [DisplayName("Suppression Duration In Days")]
        [Description("DevSkim allows for findings to be suppressed for a temporary period of time. " +
    "The default is 30 days. Set to 0 to disable temporary suppressions.")]
        public int SuppressionDurationInDays
        {
            get => StaticSettings.portableSettings.SuppressionDuration;
            set
            {
                StaticSettings.portableSettings.SuppressionDuration = value;
            }
        }

        public enum CommentStylesEnum
        {
            Line,
            Block
        }
        [Category(SuppressionsCategory)]
        [DisplayName("Suppression Comment Style")]
        [Description("When DevSkim inserts a suppression comment it defaults to using single line comments for " +
            "every language that has them.  Setting this to 'block' will instead use block comments for the languages " +
            "that support them.  Block comments are suggested if regularly adding explanations for why a finding " +
            "was suppressed")]
        public CommentStylesEnum SuppressionCommentStyle
        {
            get
            {
                if (Enum.TryParse(StaticSettings.portableSettings.SuppressionStyle, out CommentStylesEnum enumRes))
                {
                    return enumRes;
                }
                return CommentStylesEnum.Line;
            }
            set
            {
                StaticSettings.portableSettings.SuppressionStyle = value.ToString();
            }
        }

        [Category(SuppressionsCategory)]
        [DisplayName("Manual Reviewer Name")]
        [Description("If set, insert this name in inserted suppression comments.")]
        public string ManualReviewerName
        {
            get => StaticSettings.portableSettings.ReviewerName;
            set
            {
                StaticSettings.portableSettings.ReviewerName = value;
            }
        }


        /// <summary>
        /// Guidance Options
        /// </summary>
        [Category(GuidanceCategory)]
        [DisplayName("Guidance Base URL")]
        [Description("Each finding has a guidance file that describes the issue and solutions in more detail.  " +
    "By default, those files live on the DevSkim github repo however, with this setting, " +
    "organizations can clone and customize that repo, and specify their own base URL for the guidance.")]
        public string GuidanceBaseURL
        {
            get => StaticSettings.portableSettings.GuidanceBaseUrl;
            set
            {
                StaticSettings.portableSettings.GuidanceBaseUrl = value;
            }
        }


        /// <summary>
        /// Ignore Options
        /// </summary>
        [Category(IgnoresCategory)]
        [DisplayName("Ignore Files")]
        [Description("Regular expressions to exclude files and folders from analysis.")]
        public List<string> IgnoreFiles
        {
            get => StaticSettings.portableSettings.IgnoreFiles.Select(x => x.ToString()).ToList();
            set
            {
                StaticSettings.portableSettings.IgnoreFiles = value.Select(x => new System.Text.RegularExpressions.Regex(x)).ToList();
            }
        }

        [Category(IgnoresCategory)]
        [DisplayName("Ignore Rules List")]
        [Description("DevSkim Rule IDs to ignore.")]
        public List<string> IgnoreRulesList
        {
            get => StaticSettings.portableSettings.IgnoreRuleIds.ToList();
            set
            {
                StaticSettings.portableSettings.IgnoreRuleIds = value;
            }
        }

        [Category(IgnoresCategory)]
        [DisplayName("Ignore Default Rules")]
        [Description("Disable all default DevSkim rules.")]
        public bool IgnoreDefaultRules
        {
            get => StaticSettings.portableSettings.IgnoreDefaultRuleSet;
            set
            {
                StaticSettings.portableSettings.IgnoreDefaultRuleSet = value;
            }
        }


        /// <summary>
        /// Finding Options
        /// </summary>
        // TODO: Do we even have a scan all files in workspace type of commmand here?
        [Category(FindingsCategory)]
        [DisplayName("Remove Findings On Close")]
        [Description("By default, when a source file is closed the findings remain in the 'Error List' window.  " +
            "Setting this value to true will cause findings to be removed from 'Error List' when the document is closed.  " +
            "Note, setting this to true will cause findings that are listed when invoking the 'Scan all files in workspace' " +
            "command to automatically clear away after a couple of minutes.")]
        public bool RemoveFindingsOnClose
        {
            get => StaticSettings.portableSettings.RemoveFindingsOnClose;
            set
            {
                StaticSettings.portableSettings.RemoveFindingsOnClose = value;
            }
        }


        /// <summary>
        /// Trigger Options
        /// </summary>
        [Category(TriggersCategory)]
        [DisplayName("Scan On Open")]
        [Description("Scan files on open.")]
        public bool ScanOnOpen
        {
            get => StaticSettings.portableSettings.ScanOnOpen;
            set
            {
                StaticSettings.portableSettings.ScanOnOpen = value;
            }
        }

        [Category(TriggersCategory)]
        [DisplayName("Scan On Save")]
        [Description("Scan files on save.")]
        public bool ScanOnSave
        {
            get => StaticSettings.portableSettings.ScanOnSave;
            set
            {
                StaticSettings.portableSettings.ScanOnSave = value;
            }
        }

        [Category(TriggersCategory)]
        [DisplayName("Scan On Change")]
        [Description("Scan files on change.")]
        public bool ScanOnChange
        {
            get => StaticSettings.portableSettings.ScanOnChange;
            set
            {
                StaticSettings.portableSettings.ScanOnChange = value;
            }
        }
    }
}
