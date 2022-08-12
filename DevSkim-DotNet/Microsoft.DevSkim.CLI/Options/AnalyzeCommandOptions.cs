using System;
using System.Collections.Generic;
using GlobExpressions;

namespace Microsoft.DevSkim.CLI.Options;

public class AnalyzeCommandOptions
{
    public IEnumerable<Glob> Globs { get; set; } = Array.Empty<Glob>();
    public string BasePath { get; set; } = string.Empty;
    public bool DisableSuppression { get; set; }
    public bool IgnoreDefaultRules { get; set; }
    public string OutputFile { get; set; } = string.Empty;
    public string OutputFileFormat { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string[] Rulespath { get; set; } = Array.Empty<string>();
    public string[] Severities { get; set; } = Array.Empty<string>();
    public bool SuppressError { get; set; }
    public bool CrawlArchives { get; set; }
    public bool DisableParallel { get; set; }
    public bool ExitCodeIsNumIssues { get; set; }
    public string OutputTextFormat { get; set; } = string.Empty;
    public bool AbsolutePaths { get; set; } = false;
    public bool RespectGitIgnore { get; set; }
    public bool SkipExcerpts { get; set; }
    public string[] Confidences { get; set; }
}