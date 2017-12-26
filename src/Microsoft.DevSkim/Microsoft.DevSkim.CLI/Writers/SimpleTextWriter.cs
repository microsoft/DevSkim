// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.DevSkim.CLI.Writers
{
    /// <summary>
    /// - %F - full path    
    /// - %L - start line number
    /// - %C - start column
    /// - %l - end line number
    /// - %c - end column
    /// - %I - location inside file
    /// - %i - match length
    /// - %R - rule id
    /// - %N - rule name
    /// - %S - severity (Critical, Important, etc.)    
    /// - %m - string match
    /// - %T - tags(comma-separated)
    /// </summary>
    public class SimpleTextWriter : Writer
    {
        public SimpleTextWriter(string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
                _formatString = "%F:%L:%C:%l:%c [%S] %R %N";            
            else
                _formatString = formatString;
        }

        public override void WriteIssue(IssueRecord issue)
        {
            string output = _formatString.Replace("%F", issue.Filename);
            output = output.Replace("%L", issue.Issue.StartLocation.Line.ToString());
            output = output.Replace("%C", issue.Issue.StartLocation.Column.ToString());
            output = output.Replace("%l", issue.Issue.EndLocation.Line.ToString());
            output = output.Replace("%c", issue.Issue.EndLocation.Column.ToString());
            output = output.Replace("%I", issue.Issue.Boundary.Index.ToString());
            output = output.Replace("%i", issue.Issue.Boundary.Length.ToString());
            output = output.Replace("%R", issue.Issue.Rule.Id);
            output = output.Replace("%N", issue.Issue.Rule.Name);
            output = output.Replace("%S", issue.Issue.Rule.Severity.ToString());
            output = output.Replace("%D", issue.Issue.Rule.Description);
            output = output.Replace("%m", issue.TextSample);
            output = output.Replace("%T", string.Join(',',issue.Issue.Rule.Tags));

            TextWriter.WriteLine(output);
        }

        public override void FlushAndClose()
        {
            TextWriter.Flush();
            TextWriter.Close();
        }

        string _formatString;
    }
}
