// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// Shim around DevSkim. Parses code applies rules
    /// </summary>
    public class SkimShim
    {
        public SkimShim()
        {
            processor = new RuleProcessor()
            {
                EnableSuppressions = true
            };            

            LoadRules();
        }

        #region Public Static Methods

        /// <summary>
        /// Reapllys settings
        /// </summary>
        public static void ApplySettings()
        {
            _instance.LoadRules();            
        }

        /// <summary>
        /// Indicates if there are more than one issue on the given line
        /// </summary>
        /// <param name="text">line of code</param>
        /// <param name="contenttype">VS Content Type</param>
        /// <returns>True if more than one issue exists</returns>
        public static bool HasMultipleProblems(string text, string contenttype)
        {                        
            var list = Analyze(text, contenttype, string.Empty)
                      .GroupBy(x => x.Rule.Id)
                      .Select(x => x.First())
                      .ToList();

            return list.Count() > 1;            
        }

        /// <summary>
        /// Analyze text for issues
        /// </summary>
        /// <param name="text">line of code</param>
        /// <param name="contenttype">VS Content Type</param>
        /// <returns>List of actionable and non-actionable issues</returns>
        public static Issue[] Analyze(string text, string contentType, string fileName)
        {                        
            return _instance.processor.Analyze(text, _instance.GetLanguageList(contentType, fileName));
        }    

        #endregion

        #region Private

        /// <summary>
        /// Get list of applicable lenguages based on file name and VS content type
        /// </summary>
        /// <param name="contentType">Visual Studio content type</param>
        /// <param name="fileName">Filename</param>
        /// <returns></returns>
        private string[] GetLanguageList(string contentType, string fileName)
        {
            string flang = Language.FromFileName(fileName);
            List<string> langs = new List<string>(ContentType.GetLanguages(contentType));                

            if (!langs.Contains(flang))
            {
                langs.Add(flang);
            }

            return langs.ToArray();
        }

        /// <summary>
        /// Reloads rules based on settings
        /// </summary>
        private void LoadRules()
        {
            Settings set = Settings.GetSettings();

            ruleset = new RuleSet();
            string rulesFile = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            rulesFile = Path.Combine(Path.Combine(rulesFile, "Content"), "devskim-rules.json");
            if (set.UseDefaultRules)
                ruleset.AddFile(rulesFile, null);

            if (set.UseCustomRules)
                ruleset.AddDirectory(set.CustomRulesPath, "custom");

            processor.Rules = ruleset;

            processor.SeverityLevel = Severity.Critical;

            if (set.EnableImportantRules) processor.SeverityLevel |= Severity.Important;
            if (set.EnableModerateRules) processor.SeverityLevel |= Severity.Moderate;
            if (set.EnableBestPracticeRules) processor.SeverityLevel |= Severity.BestPractice;
            if (set.EnableManualReviewRules) processor.SeverityLevel |= Severity.ManualReview;
        }

        private RuleProcessor processor = new RuleProcessor();
        private RuleSet ruleset;

        private static SkimShim _instance = new SkimShim();

        #endregion
    }
}
