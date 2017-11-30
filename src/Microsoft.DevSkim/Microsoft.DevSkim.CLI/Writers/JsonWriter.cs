// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class JsonWriter : Writer
    {
        public override void WriteIssue(IssueRecord issue)
        {
            // Store the result in the result list
            jsonResult.Add(new Dictionary<string, string>()
                            {
                                { "filename", issue.Filename},

                                { "start_line", issue.Issue.StartLocation.Line.ToString() },
                                { "start_column", issue.Issue.StartLocation.Column.ToString() },
                                { "end_line", issue.Issue.EndLocation.Line.ToString() },
                                { "end_column", issue.Issue.EndLocation.Column.ToString() },
                                { "matching_section", issue.TextSample },
                                { "rule_id", issue.Issue.Rule.Id },
                                { "rule_name", issue.Issue.Rule.Name },
                                { "rule_description", issue.Issue.Rule.Description }
                            });
        }

        public override void FlushAndClose()
        {
            TextWriter.Write(JsonConvert.SerializeObject(jsonResult, Formatting.Indented));
            TextWriter.Flush();
            TextWriter.Close();
        }

        // Store the results here (JSON only)
        List<Dictionary<string, string>> jsonResult = new List<Dictionary<string, string>>();
    }
}
