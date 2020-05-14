// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using Microsoft.DevSkim.VSExtension.Utils;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// Shim around DevSkim. Parses code applies rules
    /// </summary>
    public class SkimShim
    {
        public SkimShim()
        {
            ruleset = new RuleSet();
            processor = new RuleProcessor(ruleset);
            LoadRules();
        }

        #region Public Static Methods

        /// <summary>
        /// Reapplys settings
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
            return Analyze(text, contenttype, string.Empty)
                      .GroupBy(x => x.Rule.Id)
                      .Count() > 1;
        }

        private static Dictionary<string, ValueTuple<DateTime, bool>> LastChecked = new Dictionary<string, ValueTuple<DateTime, bool>>();

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] String[] ppszOtherDirs);

        private static bool IsIgnored(string path)
        {
            int MAX_PATH = 260;

            StringBuilder sb = new StringBuilder(string.Empty, MAX_PATH);
            bool found = PathFindOnPath(sb, null);
            if (found)
            {
                ExternalCommandRunner.RunExternalCommand("git", out string stdOut, out string _, "check-ignore", path);
                if (stdOut.Contains(path.Split(Path.DirectorySeparatorChar).Last()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Analyze text for issues
        /// </summary>
        /// <param name="text">line of code</param>
        /// <param name="contenttype">VS Content Type</param>
        /// <returns>List of actionable and non-actionable issues</returns>
        public static Issue[] Analyze(string text, string contentType, string fileName = "")
        {
            Settings set = Settings.GetSettings();
            if (set.UseGitIgnore)
            {
                if (IsIgnored(fileName))
                {
                    return Array.Empty<Issue>();
                }
            }
            return _instance.processor.Analyze(text, _instance.GetLanguageList(contentType, fileName));

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

            Assembly assembly = Assembly.GetAssembly(typeof(Boundary));
            string filePath = "Microsoft.DevSkim.Resources.devskim-rules.json";
            Stream resource = assembly.GetManifestResourceStream(filePath);

            if (set.UseDefaultRules)
            {
                using (StreamReader file = new StreamReader(resource))
                {
                    ruleset.AddString(file.ReadToEnd(), filePath);
                }
            }

            if (set.UseCustomRules)
                ruleset.AddDirectory(set.CustomRulesPath, "custom");

            processor.Rules = ruleset;

            processor.SeverityLevel = Severity.Critical;

            if (set.EnableImportantRules) processor.SeverityLevel |= Severity.Important;
            if (set.EnableModerateRules) processor.SeverityLevel |= Severity.Moderate;
            if (set.EnableBestPracticeRules) processor.SeverityLevel |= Severity.BestPractice;
            if (set.EnableManualReviewRules) processor.SeverityLevel |= Severity.ManualReview;
        }

        private RuleProcessor processor;
        private RuleSet ruleset;

        private static SkimShim _instance = new SkimShim();

        #endregion
    }
}
