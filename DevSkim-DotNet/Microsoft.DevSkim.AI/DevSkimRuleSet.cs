// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInspector.RulesEngine;

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
            DevSkimRuleSet ruleSet = new DevSkimRuleSet();
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string resName in resNames.Where(x => x.StartsWith("Microsoft.DevSkim.rules.default")))
            {
                Stream? resource = assembly.GetManifestResourceStream(resName);
                using StreamReader file = new StreamReader(resource ?? new MemoryStream());
                ruleSet.AddString(file.ReadToEnd(), resName, null);
            }

            return ruleSet;
        }
    }
}
