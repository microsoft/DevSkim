// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using Microsoft.Extensions.CommandLineUtils;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DevSkim.CLI.Options;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class FixCommand
    {
        private readonly FixCommandOptions _opts;

        public FixCommand(FixCommandOptions options)
        {
            _opts = options;
        }

        public int Run()
        {
            var sarifLog = SarifLog.Load(_opts.SarifInput);
            if (sarifLog.Runs.Count > 0)
            {
                var run = sarifLog.Runs[0];
                var groupedResults = run.Results.GroupBy(x => x.Locations[0].PhysicalLocation.ArtifactLocation.Uri).ToList();
                foreach (var resultGroup in groupedResults)
                {
                    var fileName = resultGroup.Key;
                    var listOfReplacements = resultGroup.SelectMany(x =>
                        x.Fixes.SelectMany(y => y.ArtifactChanges).SelectMany(z => z.Replacements)).ToList();
                    // Order the results by the character offset
                    listOfReplacements.Sort((a, b) => a.DeletedRegion.CharOffset - b.DeletedRegion.CharOffset);
                    
                    // TODO: Support path rooting
                    if (File.Exists(fileName.AbsolutePath))
                    {
                        var theContent = File.ReadAllText(fileName.AbsolutePath);
                        // CurPos tracks the current position in the original string
                        int curPos = 0;
                        var sb = new StringBuilder();
                        foreach (var replacement in listOfReplacements)
                        {
                            // The replacements were sorted, so this indicates a second replacement option for the same region
                            // TODO: Improve
                            if (replacement.DeletedRegion.CharOffset < curPos)
                            {
                                continue;
                            }
                            sb.Append(theContent[curPos..replacement.DeletedRegion.CharOffset]);
                            sb.Append(replacement.InsertedContent.Text);
                            curPos = replacement.DeletedRegion.CharOffset + replacement.DeletedRegion.CharLength;
                        }

                        sb.Append(theContent[curPos..]);
                        File.WriteAllText(fileName.AbsolutePath, sb.ToString());
                    }
                }
            }
            
            return (int)ExitCode.NoIssues;
        }
    }
}