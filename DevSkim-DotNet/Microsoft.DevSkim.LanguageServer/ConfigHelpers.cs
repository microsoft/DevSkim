using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevSkim.LanguageServer;

internal class ConfigHelpers
{
	internal static readonly string Section = "MS-CST-E.vscode-devskim";
	internal static void SetScannerSettings(IConfiguration configuration)
	{
		StaticScannerSettings.RuleProcessorOptions = OptionsFromConfiguration(configuration);
		StaticScannerSettings.IgnoreRuleIds = configuration.GetValue<string[]>($"{Section}:ignores:ignoreRuleList");
		StaticScannerSettings.IgnoreFiles = configuration.GetValue<string[]>($"{Section}:ignores:ignoreFiles");
		StaticScannerSettings.RemoveFindingsOnClose = configuration.GetValue<bool>($"{Section}:findings:removeFindingsOnClose");
		StaticScannerSettings.ScanOnOpen = configuration.GetValue<bool>($"{Section}:triggers:scanOnOpen");
		StaticScannerSettings.ScanOnSave = configuration.GetValue<bool>($"{Section}:triggers:scanOnSave");
		StaticScannerSettings.ScanOnChange = configuration.GetValue<bool>($"{Section}:triggers:scanOnChange");
	}

	private static DevSkimRuleProcessorOptions OptionsFromConfiguration(IConfiguration configuration)
	{
		var languagesPath = configuration.GetValue<string>($"{Section}:rules:customLanguagesPath");
		var commentsPath = configuration.GetValue<string>($"{Section}:rules:customCommentsPath");
		var severityFilter = Severity.Moderate | Severity.Critical | Severity.Important;
		if (configuration.GetValue<bool>($"{Section}:rules:enableManualReviewRules"))
		{
			severityFilter |= Severity.ManualReview;
		}
		if (configuration.GetValue<bool>($"{Section}:rules:enableBestPracticeRules"))
		{
			severityFilter |= Severity.BestPractice;
		}
		if (configuration.GetValue<bool>($"{Section}:rules:enableUnspecifiedSeverityRules"))
		{
			severityFilter |= Severity.Unspecified;
		}
		var confidenceFilter = Confidence.Medium | Confidence.High;
		if (configuration.GetValue<bool>($"{Section}:rules:enableUnspecifiedConfidenceRules"))
		{
			confidenceFilter |= Confidence.Unspecified;
		}
		if (configuration.GetValue<bool>($"{Section}:rules:enableLowConfidenceRules"))
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