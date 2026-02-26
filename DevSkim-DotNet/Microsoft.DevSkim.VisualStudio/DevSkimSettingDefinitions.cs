// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.VisualStudio;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

#pragma warning disable VSEXTPREVIEW_SETTINGS

/// <summary>
/// Defines the settings for the DevSkim Visual Studio extension using the VS Extensibility SDK settings API.
/// Settings mirror <see cref="LanguageProtoInterop.PortableScannerSettings"/> properties.
/// </summary>
internal static class DevSkimSettingDefinitions
{
    #region Categories

    [VisualStudioContribution]
    internal static SettingCategory DevSkimCategory { get; } = new("devskim", "%devskim.DisplayName%")
    {
        Description = "%devskim.Description%",
        GenerateObserverClass = true,
    };

    [VisualStudioContribution]
    internal static SettingCategory RulesCategory { get; } = new("devskimRules", "%devskim.rules.DisplayName%", DevSkimCategory)
    {
        Description = "%devskim.rules.Description%",
    };

    [VisualStudioContribution]
    internal static SettingCategory SuppressionsCategory { get; } = new("devskimSuppressions", "%devskim.suppressions.DisplayName%", DevSkimCategory)
    {
        Description = "%devskim.suppressions.Description%",
    };

    [VisualStudioContribution]
    internal static SettingCategory TriggersCategory { get; } = new("devskimTriggers", "%devskim.triggers.DisplayName%", DevSkimCategory)
    {
        Description = "%devskim.triggers.Description%",
    };

    [VisualStudioContribution]
    internal static SettingCategory IgnoresCategory { get; } = new("devskimIgnores", "%devskim.ignores.DisplayName%", DevSkimCategory)
    {
        Description = "%devskim.ignores.Description%",
    };

    #endregion

    #region Rules Settings - Severity

    [VisualStudioContribution]
    internal static Setting.Boolean EnableCriticalSeverityRules { get; } = new("devskimEnableCriticalSeverityRules", "%devskim.rules.enableCriticalSeverityRules.DisplayName%", RulesCategory, defaultValue: true)
    {
        Description = "%devskim.rules.enableCriticalSeverityRules.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean EnableImportantSeverityRules { get; } = new("devskimEnableImportantSeverityRules", "%devskim.rules.enableImportantSeverityRules.DisplayName%", RulesCategory, defaultValue: true)
    {
        Description = "%devskim.rules.enableImportantSeverityRules.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean EnableModerateSeverityRules { get; } = new("devskimEnableModerateSeverityRules", "%devskim.rules.enableModerateSeverityRules.DisplayName%", RulesCategory, defaultValue: true)
    {
        Description = "%devskim.rules.enableModerateSeverityRules.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean EnableManualReviewSeverityRules { get; } = new("devskimEnableManualReviewSeverityRules", "%devskim.rules.enableManualReviewSeverityRules.DisplayName%", RulesCategory, defaultValue: true)
    {
        Description = "%devskim.rules.enableManualReviewSeverityRules.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean EnableBestPracticeSeverityRules { get; } = new("devskimEnableBestPracticeSeverityRules", "%devskim.rules.enableBestPracticeSeverityRules.DisplayName%", RulesCategory, defaultValue: true)
    {
        Description = "%devskim.rules.enableBestPracticeSeverityRules.Description%",
    };

    #endregion

    #region Rules Settings - Confidence

    [VisualStudioContribution]
    internal static Setting.Boolean EnableHighConfidenceRules { get; } = new("devskimEnableHighConfidenceRules", "%devskim.rules.enableHighConfidenceRules.DisplayName%", RulesCategory, defaultValue: true)
    {
        Description = "%devskim.rules.enableHighConfidenceRules.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean EnableMediumConfidenceRules { get; } = new("devskimEnableMediumConfidenceRules", "%devskim.rules.enableMediumConfidenceRules.DisplayName%", RulesCategory, defaultValue: true)
    {
        Description = "%devskim.rules.enableMediumConfidenceRules.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean EnableLowConfidenceRules { get; } = new("devskimEnableLowConfidenceRules", "%devskim.rules.enableLowConfidenceRules.DisplayName%", RulesCategory, defaultValue: false)
    {
        Description = "%devskim.rules.enableLowConfidenceRules.Description%",
    };

    #endregion

    #region Rules Settings - Paths and Configuration

    [VisualStudioContribution]
    internal static Setting.Boolean IgnoreDefaultRules { get; } = new("devskimIgnoreDefaultRules", "%devskim.rules.ignoreDefaultRules.DisplayName%", RulesCategory, defaultValue: false)
    {
        Description = "%devskim.rules.ignoreDefaultRules.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.String CustomRulesPaths { get; } = new("devskimCustomRulesPaths", "%devskim.rules.customRulesPaths.DisplayName%", RulesCategory, defaultValue: "")
    {
        Description = "%devskim.rules.customRulesPaths.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.String CustomLanguagesPath { get; } = new("devskimCustomLanguagesPath", "%devskim.rules.customLanguagesPath.DisplayName%", RulesCategory, defaultValue: "")
    {
        Description = "%devskim.rules.customLanguagesPath.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.String CustomCommentsPath { get; } = new("devskimCustomCommentsPath", "%devskim.rules.customCommentsPath.DisplayName%", RulesCategory, defaultValue: "")
    {
        Description = "%devskim.rules.customCommentsPath.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.String GuidanceBaseURL { get; } = new("devskimGuidanceBaseURL", "%devskim.rules.guidanceBaseURL.DisplayName%", RulesCategory, defaultValue: "https://github.com/microsoft/devskim/tree/main/guidance")
    {
        Description = "%devskim.rules.guidanceBaseURL.Description%",
    };

    #endregion

    #region Suppressions Settings

    [VisualStudioContribution]
    internal static Setting.String SuppressionCommentStyle { get; } = new("devskimSuppressionCommentStyle", "%devskim.suppressions.suppressionCommentStyle.DisplayName%", SuppressionsCategory, defaultValue: "Line")
    {
        Description = "%devskim.suppressions.suppressionCommentStyle.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.String ManualReviewerName { get; } = new("devskimManualReviewerName", "%devskim.suppressions.manualReviewerName.DisplayName%", SuppressionsCategory, defaultValue: "")
    {
        Description = "%devskim.suppressions.manualReviewerName.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Integer SuppressionDurationInDays { get; } = new("devskimSuppressionDurationInDays", "%devskim.suppressions.suppressionDurationInDays.DisplayName%", SuppressionsCategory, defaultValue: 30)
    {
        Description = "%devskim.suppressions.suppressionDurationInDays.Description%",
    };

    #endregion

    #region Triggers Settings

    [VisualStudioContribution]
    internal static Setting.Boolean ScanOnOpen { get; } = new("devskimScanOnOpen", "%devskim.triggers.scanOnOpen.DisplayName%", TriggersCategory, defaultValue: true)
    {
        Description = "%devskim.triggers.scanOnOpen.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean ScanOnSave { get; } = new("devskimScanOnSave", "%devskim.triggers.scanOnSave.DisplayName%", TriggersCategory, defaultValue: true)
    {
        Description = "%devskim.triggers.scanOnSave.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean ScanOnChange { get; } = new("devskimScanOnChange", "%devskim.triggers.scanOnChange.DisplayName%", TriggersCategory, defaultValue: true)
    {
        Description = "%devskim.triggers.scanOnChange.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean RemoveFindingsOnClose { get; } = new("devskimRemoveFindingsOnClose", "%devskim.triggers.removeFindingsOnClose.DisplayName%", TriggersCategory, defaultValue: true)
    {
        Description = "%devskim.triggers.removeFindingsOnClose.Description%",
    };

    #endregion

    #region Ignores Settings

    [VisualStudioContribution]
    internal static Setting.String IgnoreRulesList { get; } = new("devskimIgnoreRulesList", "%devskim.ignores.ignoreRulesList.DisplayName%", IgnoresCategory, defaultValue: "")
    {
        Description = "%devskim.ignores.ignoreRulesList.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.String IgnoreFiles { get; } = new("devskimIgnoreFiles", "%devskim.ignores.ignoreFiles.DisplayName%", IgnoresCategory, defaultValue: "")
    {
        Description = "%devskim.ignores.ignoreFiles.Description%",
    };

    #endregion

    #region Diagnostics Settings

    [VisualStudioContribution]
    internal static SettingCategory DiagnosticsCategory { get; } = new("devskimDiagnostics", "%devskim.diagnostics.DisplayName%", DevSkimCategory)
    {
        Description = "%devskim.diagnostics.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean EnableFileLogging { get; } = new("devskimEnableFileLogging", "%devskim.diagnostics.enableFileLogging.DisplayName%", DiagnosticsCategory, defaultValue: false)
    {
        Description = "%devskim.diagnostics.enableFileLogging.Description%",
    };

    #endregion
}
