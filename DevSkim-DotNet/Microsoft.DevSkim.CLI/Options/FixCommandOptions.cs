﻿using System;
using System.Collections.Generic;
using CommandLine;

namespace Microsoft.DevSkim.CLI.Options;

[Verb("fix", HelpText = "Apply fixes from a Sarif")]
public class FixCommandOptions
{
    [Option('I',Required = true, HelpText = "Path to source code")]
    public string Path { get; set; } = String.Empty;
    [Option('O',Required = true, HelpText = "Filename for input sarif with proposed fixes.")]
    public string SarifInput { get; set; } = String.Empty;
    [Option("all", HelpText = "Apply all fixes.")]
    public bool ApplyAllFixes { get; set; }
    [Option("files", HelpText = "Comma separated list of paths to apply fixes to", Separator = ',')]
    public IEnumerable<string> FilesToApplyTo { get; set; } = Array.Empty<string>();
    [Option("rules", HelpText = "Comma separated list of rules to apply fixes for", Separator = ',')]
    public IEnumerable<string> RulesToApplyFrom { get; set; } = Array.Empty<string>();
}