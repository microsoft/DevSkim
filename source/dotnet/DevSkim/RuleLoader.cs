// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Security.DevSkim
{
    /// <summary>
    /// Provides functionality for loading the rules
    /// </summary>
    public class RuleLoader
    {
        /// <summary>
        /// Parse a directory with rules files and loads the rules
        /// </summary>
        /// <param name="path">Path to rules folder</param>
        /// <param name="tag">Tag for the rules</param>
        /// <returns>Return list of Rules objects</returns>
        public static List<Rule> ParseDirectory(string path, string tag)
        {
            if (path == null)
                throw new ArgumentNullException();

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            List<Rule> result = new List<Rule>();

            foreach (string fileName in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                List<Rule> ruleList = new List<Rule>();
                using (StreamReader file = File.OpenText(fileName))
                {
                    ruleList = JsonConvert.DeserializeObject<List<Rule>>(file.ReadToEnd());
                    foreach (Rule r in ruleList)
                    {                            
                        r.File = fileName;
                        r.Tag = tag;

                        foreach (SearchPattern p in r.Patterns)
                        {
                            if (p.Type == PatternType.Regex_Word || p.Type == PatternType.String)
                            {
                                p.Type = PatternType.Regex;
                                p.Pattern = string.Format(@"\b{0}\b", p.Pattern);
                            }
                        }
                    }

                    // Add only active rules
                    foreach(Rule r in ruleList)
                    {
                        if (r.Active)
                            result.Add(r);
                    }
                }
            }

            return result;
        }
    }
}
