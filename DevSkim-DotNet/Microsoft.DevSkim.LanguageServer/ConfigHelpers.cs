using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevSkim.LanguageServer;

internal class ConfigHelpers
{
	/// <summary>
	/// Lists are presented in the configuration as a number of items with the name of the list appended with ':0' where 0 is the index of the item from the list.
	/// This method compacts those back to a Collection for convenience.
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="subSection"></param>
	/// <returns></returns>
	internal static ICollection<string> GetStringList(IConfiguration configuration, string subSection)
	{
		var toReturn = new List<string>();
		int i = 0;
		while (true)
		{
			string nextIgnoreRuleId = configuration.GetValue<string>($"{Section}:{subSection}:{i}");
            if (nextIgnoreRuleId == null)
			{
				break;
			}
			else
			{
                toReturn.Add(nextIgnoreRuleId);
				i++;
			}
		}
		return toReturn;
	}

	internal static readonly string Section = "MS-CST-E.vscode-devskim";
	internal static void SetScannerSettings(IConfiguration configuration)
	{
		StaticScannerSettings.RuleProcessorOptions = OptionsFromConfiguration(configuration);
        StaticScannerSettings.IgnoreDefaultRuleSet = configuration.GetValue<bool>($"{Section}:ignores:ignoreDefaultRules");
		StaticScannerSettings.CustomRulePaths = GetStringList(configuration, "rules:customRulesPaths");
		StaticScannerSettings.IgnoreRuleIds = GetStringList(configuration, "ignores:ignoreRuleList");

		// TODO: TextDocumentSyncHandler should ignore these
        StaticScannerSettings.IgnoreFiles = GetStringList(configuration, "ignores:ignoreFiles");

        StaticScannerSettings.RemoveFindingsOnClose = configuration.GetValue<bool>($"{Section}:findings:removeFindingsOnClose");
		StaticScannerSettings.ScanOnOpen = configuration.GetValue<bool>($"{Section}:triggers:scanOnOpen");
		StaticScannerSettings.ScanOnSave = configuration.GetValue<bool>($"{Section}:triggers:scanOnSave");
		StaticScannerSettings.ScanOnChange = configuration.GetValue<bool>($"{Section}:triggers:scanOnChange");

		var ruleSet = StaticScannerSettings.IgnoreDefaultRuleSet ? new DevSkimRuleSet() : DevSkimRuleSet.GetDefaultRuleSet();
		foreach (var path in StaticScannerSettings.CustomRulePaths)
		{
			ruleSet.AddPath(path);
		}
		ruleSet = ruleSet.WithoutIds(StaticScannerSettings.IgnoreRuleIds);
		StaticScannerSettings.RuleSet = ruleSet;
		StaticScannerSettings.Processor = new DevSkimRuleProcessor(StaticScannerSettings.RuleSet, StaticScannerSettings.RuleProcessorOptions);
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
			LoggerFactory = NullLoggerFactory.Instance,
		};
	}
}