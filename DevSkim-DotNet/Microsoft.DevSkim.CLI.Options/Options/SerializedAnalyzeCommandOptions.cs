using System.Collections.Generic;
using CommandLine;

namespace Microsoft.DevSkim.CLI.Options;

/// <summary>
/// A serializable options object to provide to the analyze command.
/// </summary>
public record SerializedAnalyzeCommandOptions : BaseAnalyzeCommandOptions
{
    /// <summary>
    /// Dictionary that maps Language name to RuleIDs to ignore
    /// </summary>
    [Option("LanguageRuleIgnoreMap", HelpText = "Mapping from language name to list of rules to ignore")]
    public IDictionary<string, List<string>> LanguageRuleIgnoreMap { get; set; } =
        new Dictionary<string, List<string>>();
}