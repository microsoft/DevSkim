using System;
using System.Collections.Generic;
using CommandLine;
using Microsoft.ApplicationInspector.RulesEngine;

namespace Microsoft.DevSkim.CLI.Options;

[Verb("analyze", HelpText = "Analyze source code using DevSkim")]
public class AnalyzeCommandOptions
{
    [Option('r', HelpText = "Comma separated list of paths to rules files to use", Separator = ',')]
    public IEnumerable<string> Rules { get; set; } = Array.Empty<string>();
    [Option('I',Required = true, HelpText = "Path to source code")]
    public string Path { get; set; } = String.Empty;
    [Option('O',"output-file",Required = true, HelpText = "Filename for result file.")]
    public string OutputFile { get; set; } = String.Empty;

    [Option('o', "output-format", Required = true, HelpText = "Format for output text.")]
    public string OutputTextFormat { get; set; } = String.Empty;
    [Option('f',"file-format", HelpText = "Format type for output. [text|sarif]", Default = "sarif")]
    public string OutputFileFormat { get; set; } = String.Empty;
    [Option('s',"severity", HelpText = "Comma-separated Severities to match", Separator = ',', Default = new[]{Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview })]
    public IEnumerable<Severity> Severities { get; set; } = new[]{Severity.Critical, Severity.Important, Severity.Moderate, Severity.BestPractice, Severity.ManualReview };
    [Option("confidence", HelpText = "Comma-separated Severities to match", Separator = ',', Default = new[]{ Confidence.High, Confidence.Medium })]
    public IEnumerable<Confidence> Confidences { get; set; } = new[]{ Confidence.High, Confidence.Medium };
    [Option('g',"ignore-globs", HelpText = "Comma-separated Globs for files to skip analyzing", Separator = ',', Default = new []{"**/.git/**","**/bin/**"})]
    public IEnumerable<string> Globs { get; set; }  = Array.Empty<string>();
    [Option('d',"disable-supression", HelpText = "Disable comment suppressions")]
    public bool DisableSuppression { get; set; }
    [Option("disable-parallel", HelpText = "Disable parallel processing")]
    public bool DisableParallel { get; set; }
    [Option('i',"ignore-default-rules", HelpText = "Ignore default rules")]
    public bool IgnoreDefaultRules { get; set; }
    [Option("suppress-standard-error", HelpText = "Suppress output to std err")]
    public bool SuppressStdErr { get; set; }
    [Option('c',"crawl-archives", HelpText = "Analyze files contained inside of archives")]
    public bool CrawlArchives { get; set; }
    [Option('E', HelpText = "Use exit code for number of issues. Negative on error.")]
    public bool ExitCodeIsNumIssues { get; set; }
    [Option("base-path",
        HelpText =
            "Specify what path to root result URIs with. When not set will generate paths relative to the source directory (or directory containing the source file specified)")]
    public string BasePath { get; set; } = string.Empty;
    [Option("absolute-path", HelpText = "Output absolute paths (overrides --base-path).")]
    public bool AbsolutePaths { get; set; }
    [Option("suppress-error", HelpText = "Don't output to stderr")]
    public bool SuppressError { get; set; }
    [Option("skip-git-ignored-files", HelpText = "Set to skip files which are ignored by .gitignore. Requires git to be installed.")]
    public bool RespectGitIgnore { get; set; }
    [Option("skip-excerpts", HelpText = "Set to skip gathering excerpts and samples to include in the report.")]
    public bool SkipExcerpts { get; set; }
}