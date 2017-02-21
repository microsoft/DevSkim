// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.Security.DevSkim
{
    /// <summary>
    /// Helper class for language based commenting
    /// </summary>
    class Language
    {
        private Language()
        {
            Assembly assembly = typeof(Microsoft.Security.DevSkim.Language).GetTypeInfo().Assembly;
            Stream resource = assembly.GetManifestResourceStream("Microsoft.Security.DevSkim.Resources.comments.json");

            using (StreamReader file = new StreamReader(resource))
            {
                Comments = JsonConvert.DeserializeObject<List<Comment>>(file.ReadToEnd());
            }
        }

        /// <summary>
        /// Decorates given string with language specific comments
        /// </summary>
        /// <param name="textToComment">text to be decorated</param>
        /// <param name="language">Language</param>
        /// <returns>Commented string</returns>
        public static string Comment(string textToComment, string language)
        {
            string result = string.Empty;

            foreach (Comment comment in Instance.Comments)
            {
                foreach (string ct in comment.ContentTypes)
                {
                    if (ct == language)
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
    }
}
