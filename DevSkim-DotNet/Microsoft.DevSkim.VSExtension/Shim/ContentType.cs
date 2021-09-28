﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    ///     Helper class for language based commenting and type converion
    /// </summary>
    public class ContentType
    {
        /// <summary>
        ///     Returns list of languages coresponding to Visual Studio content type
        /// </summary>
        /// <param name="vsContentType"> Content Type </param>
        /// <returns> List of programming languages </returns>
        public static string[] GetLanguages(string vsContentType)
        {
            foreach (ContentTypeRecord record in _instance.ContentTypes)
            {
                if (record.VSType == vsContentType)
                {
                    return record.DSTypes;
                }
            }

            return new string[] { };
        }

        private static ContentType _instance = new ContentType();

        private List<ContentTypeRecord> ContentTypes;

        private ContentType()
        {
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string fileName = Path.Combine(Path.Combine(dir, "Content"), "ContentTypes.json");
            using (StreamReader file = File.OpenText(fileName))
            {
                ContentTypes = JsonSerializer.Deserialize<List<ContentTypeRecord>>(file.ReadToEnd());
            }
        }
    }
}