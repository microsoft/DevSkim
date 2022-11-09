using System;
using System.Collections.Generic;
using CommandLine;

namespace Microsoft.DevSkim.CLI.Options;

[Verb("verify", HelpText = "Verify rule validity")]
public class VerifyCommandOptions
{
    [Option('r', HelpText = "Comma separated list of paths to rules files to use", Separator = ',')]
    public IEnumerable<string> Rules { get; set; } = Array.Empty<string>();
}