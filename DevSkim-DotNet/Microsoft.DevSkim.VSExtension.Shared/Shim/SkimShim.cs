// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.DevSkim.VSExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    ///     Shim around DevSkim. Parses code applies rules
    /// </summary>
    public class SkimShim
    {
        public SkimShim()
        {
            ruleset = new RuleSet();
            processor = new RuleProcessor(ruleset, new RuleProcessorOptions() { EnableSuppressions = true }) ;
            LoadRules();
        }

        /// <summary>
        ///     Analyze text for issues
        /// </summary>
        /// <param name="text"> line of code </param>
        /// <param name="contenttype"> VS Content Type </param>
        /// <returns> List of actionable and non-actionable issues </returns>
        public static Issue[] Analyze(string text, string contentType, string fileName = "")
        {
            Settings set = Settings.GetSettings();
            if (set.UseGitIgnore)
            {
                if (helper.IsIgnored(fileName))
                {
                    return Array.Empty<Issue>();
                }
            }

            return _instance.processor.Analyze(text, _instance.GetLanguageList(contentType, fileName));
        }

        /// <summary>
        ///     Reapplys settings
        /// </summary>
        public static void ApplySettings()
        {
            _instance.LoadRules();
        }

        /// <summary>
        ///     Indicates if there are more than one issue on the given line
        /// </summary>
        /// <param name="text"> line of code </param>
        /// <param name="contenttype"> VS Content Type </param>
        /// <returns> True if more than one issue exists </returns>
        public static bool HasMultipleProblems(string text, string contenttype)
        {
            return Analyze(text, contenttype, string.Empty)
                      .GroupBy(x => x.Rule.Id)
                      .Select(x => x.First())
                      .Count() > 1;
        }

        private static SkimShim _instance = new SkimShim();
        private static GitHelper helper = new GitHelper();
        private RuleProcessor processor;

        private RuleSet ruleset;

        /// <summary>
        ///     Get list of applicable lenguages based on file name and VS content type
        /// </summary>
        /// <param name="contentType"> Visual Studio content type </param>
        /// <param name="fileName"> Filename </param>
        /// <returns> </returns>
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
        ///     Reloads rules based on settings
        /// </summary>
        private void LoadRules()
        {
            Settings set = Settings.GetSettings();

            try
            {

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
            }
            catch(Exception e){
                Debug.WriteLine("Failed to load Default rules. {0}:{1}\n{2}",e.GetType(), e.Message, e.StackTrace);
            }
            
            try {
                if (set.UseCustomRules && Directory.Exists(set.CustomRulesPath))
                    ruleset.AddDirectory(set.CustomRulesPath, "custom");
            }
            catch(Exception e){
                Debug.WriteLine("Failed to load custom rules. {0}:{1}\n{2}",e.GetType(), e.Message, e.StackTrace);
            }

            processor.Rules = ruleset;

            processor.ProcessorOptions.SeverityLevel = Severity.Critical;

            if (set.EnableImportantRules) processor.ProcessorOptions.SeverityLevel |= Severity.Important;
            if (set.EnableModerateRules) processor.ProcessorOptions.SeverityLevel |= Severity.Moderate;
            if (set.EnableBestPracticeRules) processor.ProcessorOptions.SeverityLevel |= Severity.BestPractice;
            if (set.EnableManualReviewRules) processor.ProcessorOptions.SeverityLevel |= Severity.ManualReview;
        }
    }
}