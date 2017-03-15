// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Helper class for language based commenting
    /// </summary>
    public class Language
    {
        private Language()
        {
            Assembly assembly = typeof(Microsoft.DevSkim.Language).GetTypeInfo().Assembly;

            // Load comments
            Stream resource = assembly.GetManifestResourceStream("Microsoft.DevSkim.Resources.comments.json");
            using (StreamReader file = new StreamReader(resource))
            {
                Comments = JsonConvert.DeserializeObject<List<Comment>>(file.ReadToEnd());
            }

            // Load languages
            resource = assembly.GetManifestResourceStream("Microsoft.DevSkim.Resources.languages.json");
            using (StreamReader file = new StreamReader(resource))
            {
                ContentTypes = JsonConvert.DeserializeObject<List<ContentType>>(file.ReadToEnd());
            }
        }

        /// <summary>
        /// Returns language for given file name
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>Language</returns>
        public static string FromFileName(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            if (ext.StartsWith(".", StringComparison.CurrentCulture))
                ext = ext.Substring(1);

            foreach (ContentType item in Instance.ContentTypes)
            {
                if (Array.Exists(item.Extensions, x => x.Equals(ext)))
                    return item.Name;
            }

            return string.Empty;
        }

        /// <summary>
        /// Decorates given string with language specific comment prefix/suffix
        /// </summary>
        /// <param name="textToComment">Text to be decorated</param>
        /// <param name="language">Language</param>
        /// <returns>Commented string</returns>
        public static string Comment(string textToComment, string language)
        {
            string result = string.Empty;

            foreach (Comment comment in Instance.Comments)
            {
                foreach (string lang in comment.Languages)
                {
                    if (lang == language)
                    {
                        result = string.Concat(comment.Preffix, textToComment, comment.Suffix);
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(result))
                    break;
            }

            return result;
        }

        private static Language _instance;
        private static Language Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Language();

                return _instance;
            }        
        }

        private List<Comment> Comments;
        private List<ContentType> ContentTypes;      
    }
}

