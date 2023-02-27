using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;

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
	internal static ICollection<T> CompileList<T>(IConfiguration configuration, string subSection)
	{
        List<T> toReturn = new List<T>();
		int i = 0;
		while (true)
		{
			T nextItem = configuration.GetValue<T>($"{Section}:{subSection}:{i}");
            if (nextItem == null)
			{
				break;
			}
			else
			{
                toReturn.Add(nextItem);
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
		StaticScannerSettings.CustomRulePaths = CompileList<string>(configuration, "rules:customRulesPaths");
		StaticScannerSettings.IgnoreRuleIds = CompileList<string>(configuration, "ignores:ignoreRuleList");
        StaticScannerSettings.IgnoreFiles = CompileList<string>(configuration, "ignores:ignoreFiles").Select(x => new Regex(x)).ToList();

        StaticScannerSettings.RemoveFindingsOnClose = configuration.GetValue<bool>($"{Section}:findings:removeFindingsOnClose");
		StaticScannerSettings.ScanOnOpen = configuration.GetValue<bool>($"{Section}:triggers:scanOnOpen");
		StaticScannerSettings.ScanOnSave = configuration.GetValue<bool>($"{Section}:triggers:scanOnSave");
		StaticScannerSettings.ScanOnChange = configuration.GetValue<bool>($"{Section}:triggers:scanOnChange");

        DevSkimRuleSet ruleSet = StaticScannerSettings.IgnoreDefaultRuleSet ? new DevSkimRuleSet() : DevSkimRuleSet.GetDefaultRuleSet();
		foreach (string path in StaticScannerSettings.CustomRulePaths)
		{
			ruleSet.AddPath(path);
		}
		ruleSet = ruleSet.WithoutIds(StaticScannerSettings.IgnoreRuleIds);
		StaticScannerSettings.RuleSet = ruleSet;
		StaticScannerSettings.Processor = new DevSkimRuleProcessor(StaticScannerSettings.RuleSet, StaticScannerSettings.RuleProcessorOptions);
	}

	private static DevSkimRuleProcessorOptions OptionsFromConfiguration(IConfiguration configuration)
	{
        string languagesPath = configuration.GetValue<string>($"{Section}:rules:customLanguagesPath");
        string commentsPath = configuration.GetValue<string>($"{Section}:rules:customCommentsPath");
        Severity severityFilter = Severity.Moderate | Severity.Critical | Severity.Important;
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
        Confidence confidenceFilter = Confidence.Medium | Confidence.High;
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