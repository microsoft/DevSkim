// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using Microsoft.Extensions.CommandLineUtils;
using System.Diagnostics;
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
            foreach (var run in sarifLog.Runs)
            {
                // foreach (Address location in run.Addresses)
                // {
                //     var resultsForLocation = run.Results.Where(x => x.Locations.First().);
                // }
                // foreach (var result in run.Results)
                // {
                //     
                // }
            }
            // Offset is incremented when applying a fix that is longer than the original
            // and reduced when applying a fix that is smaller than the original
            int offset = 0;
            // StringBuilder fileTextRebuilder = new StringBuilder();
            // if (opts.ApplyFixes)
            // {
            //     fileTextRebuilder.Append(fileText);
            // }
            // Iterate through each issue
            // // Can't change files in archives, so we need the actual path to exist on disc
            // if (opts.ApplyFixes && issue.Rule.Fixes?.Any() is true)
            // {
            //     if (File.Exists(fileEntry.FullPath))
            //     {
            //         var theFixToUse = issue.Rule.Fixes[0];
            //         var theIssueIndexWithOffset = issue.Boundary.Index + offset;
            //         var theIssueLength = issue.Boundary.Length;
            //         var theTargetToFix = fileText[theIssueIndexWithOffset..(theIssueIndexWithOffset + theIssueLength)];
            //         var theFixedTarget = DevSkimRuleProcessor.Fix(theTargetToFix, theFixToUse);
            //         fileText = $"{fileText[..theIssueIndexWithOffset]}{theFixedTarget}{fileText[(theIssueIndexWithOffset + theFixedTarget.Length)..]}";
            //         offset += (theFixedTarget.Length - theIssueLength);
            //         record.Fixed = true;
            //         record.FixApplied = theFixToUse;
            //     }
            //     else
            //     {
            //         Debug.WriteLine("{0} appears to be a file located inside an archive so fixed will have to be applied manually.");
            //     }
            // }
            return (int)ExitCode.NoIssues;
        }
    }
}