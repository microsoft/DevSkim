using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevSkim.LanguageServer;

internal class ConfigHelpers
{
	internal static void SetScannerSettings(IConfiguration configuration)
	{
		StaticScannerSettings.RuleProcessorOptions = OptionsFromConfiguration(configuration);
		StaticScannerSettings.IgnoreRuleIds = configuration.GetValue<string[]>("MS-CST-E.vscode-devskim:ignores:ignoreRuleList");
		StaticScannerSettings.IgnoreFiles = configuration.GetValue<string[]>("MS-CST-E.vscode-devskim:ignores:ignoreFiles");
		StaticScannerSettings.RemoveFindingsOnClose = configuration.GetValue<bool>("MS-CST-E.vscode-devskim:findings:removeFindingsOnClose");
		StaticScannerSettings.ScanOnOpen = configuration.GetValue<bool>("MS-CST-E.vscode-devskim:triggers:scanOnOpen");
		StaticScannerSettings.ScanOnSave = configuration.GetValue<bool>("MS-CST-E.vscode-devskim:triggers:scanOnSave");
		StaticScannerSettings.ScanOnChange = configuration.GetValue<bool>("MS-CST-E.vscode-devskim:triggers:scanOnChange");
	}

	private static DevSkimRuleProcessorOptions OptionsFromConfiguration(IConfiguration configuration)
	{
		var languagesPath = configuration.GetValue<string>("MS-CST-E.vscode-devskim:rules:customLanguagesPath");
		var commentsPath = configuration.GetValue<string>("MS-CST-E.vscode-devskim:rules:customCommentsPath");
		var severityFilter = Severity.Moderate | Severity.Critical | Severity.Important;
		if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim:rules:enableManualReviewRules"))
		{
			severityFilter |= Severity.ManualReview;
		}
		if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim:rules:enableBestPracticeRules"))
		{
			severityFilter |= Severity.BestPractice;
		}
		if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim:rules:enableUnspecifiedSeverityRules"))
		{
			severityFilter |= Severity.Unspecified;
		}
		var confidenceFilter = Confidence.Medium | Confidence.High;
		if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim:rules:enableUnspecifiedConfidenceRules"))
		{
			confidenceFilter |= Confidence.Unspecified;
		}
		if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim:rules:enableLowConfidenceRules"))
		{
			confidenceFilter |= Confidence.Low;
		}
		return new DevSkimRuleProcessorOptions()
		{
			Languages = (string.IsNullOrEmpty(languagesPath) || string.IsNullOrEmpty(commentsPath)) ? DevSkimLanguages.LoadEmbedded() : DevSkimLanguages.FromFiles(commentsPath, languagesPath),
			SeverityFilter = severityFilter,
			ConfidenceFilter = confidenceFilter,
			LoggerFactory = NullLoggerFactory.Instance
		};
	}
}