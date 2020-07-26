// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;

namespace Microsoft.CST.OAT.Utils
{
    /// <summary>
    /// Localized string fetcher
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// Get the string at the key specified in the current locale
        /// </summary>
        /// <param name="key">The key to get</param>
        /// <returns>A string looked up from the table or the key itself if not present</returns>
        public static string Get(string key)
        {
            if (stringList.ContainsKey(key))
            {
                return stringList[key];
            }
            return key;
        }

        /// <summary>
        /// Load the specified locale's resources.  Currently only "" is supported for English.
        /// </summary>
        /// <param name="locale">The name of the locale</param>
        public static void Setup(string locale = "")
        {
            if (string.IsNullOrEmpty(locale))
            {
                using var stream = typeof(Rule).Assembly.GetManifestResourceStream("LogicalAnalyzer.Resources.resources");
                if (stream is Stream)
                {
                    stringList.Clear();
                    foreach (DictionaryEntry? entry in new ResourceReader(stream))
                    {
                        if (entry is DictionaryEntry dictionaryEntry)
                        {
                            var keyStr = dictionaryEntry.Key.ToString();
                            var valueStr = dictionaryEntry.Value?.ToString();
                            if (keyStr is string && valueStr is string)
                                stringList.Add(keyStr, valueStr);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// If there is any data available in the internal dictionary
        /// </summary>
        public static bool IsLoaded => stringList.Any();

        /// <summary>
        ///     Internal member structure holding string resources
        /// </summary>
        private static readonly Dictionary<string, string> stringList = new Dictionary<string, string>();
    }
}