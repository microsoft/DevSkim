﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class JsonWriter : Writer
    {
        public JsonWriter(string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
                _formatString = "%F%L%C%l%c%m%S%R%N%D";
            else
                _formatString = formatString;
        }

        public override void WriteIssue(IssueRecord issue)
        {
            Dictionary<string, object> item = new Dictionary<string, object>();
            
            if (_formatString.Contains("%F"))
                item.Add("filename", issue.Filename);
            if (_formatString.Contains("%L"))
                item.Add("start_line", issue.Issue.StartLocation.Line);
            if (_formatString.Contains("%C"))
                item.Add("start_column", issue.Issue.StartLocation.Column);
            if (_formatString.Contains("%l"))
                item.Add("end_line", issue.Issue.EndLocation.Line);
            if (_formatString.Contains("%c"))
                item.Add("end_column", issue.Issue.EndLocation.Column);
            if (_formatString.Contains("%I"))
                item.Add("match_index", issue.Issue.Boundary.Index);
            if (_formatString.Contains("%i"))                
                item.Add("match_length", issue.Issue.Boundary.Length);
            if (_formatString.Contains("%R"))
                item.Add("rule_id", issue.Issue.Rule.Id);
            if (_formatString.Contains("%N"))
                item.Add("rule_name", issue.Issue.Rule.Name);
            if (_formatString.Contains("%S"))
                item.Add("severity", issue.Issue.Rule.Severity);
            if (_formatString.Contains("%D"))
                item.Add("description", issue.Issue.Rule.Description);
            if (_formatString.Contains("%m"))
                item.Add("match", issue.TextSample);
            if (_formatString.Contains("%T"))
                item.Add("tags", issue.Issue.Rule.Tags);

            // Store the result in the result list
            jsonResult.Add(item);
        }

        public override void FlushAndClose()
        {
            TextWriter.Write(JsonConvert.SerializeObject(jsonResult, Formatting.Indented));
            TextWriter.Flush();
            TextWriter.Close();
        }

        // Store the results here (JSON only)
        List<Dictionary<string, object>> jsonResult = new List<Dictionary<string, object>>();
        string _formatString;
    }
}
