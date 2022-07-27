// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.CST.OAT;
using System.Text.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Rule = Microsoft.CST.OAT.Rule;

namespace Microsoft.DevSkim.AI
{
    /// <summary>
    ///     Storage for rules
    /// </summary>
    public class DevSkimRuleSet : TypedRuleSet<DevSkimRule>
    {
        /// <summary>
        ///     Creates instance of Ruleset
        /// </summary>
        public DevSkimRuleSet()
        {
        }

        public static DevSkimRuleSet GetDefaultRuleSet()
        {
            DevSkimRuleSet ruleSet = new();
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string resName in resNames.Where(x => x.StartsWith("Microsoft.DevSkim.rules.default")))
            {
                Stream? resource = assembly.GetManifestResourceStream(resName);
                using StreamReader file = new(resource ?? new MemoryStream());
                ruleSet.AddString(file.ReadToEnd(), resName, null);
            }

            return ruleSet;
        }
    }
}
