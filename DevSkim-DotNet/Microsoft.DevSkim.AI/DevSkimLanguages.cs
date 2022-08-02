using System.IO;
using System.Reflection;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;

namespace Microsoft.DevSkim.AI
{
    public static class DevSkimLanguages
    {
        public static Languages LoadEmbedded(ILoggerFactory? loggerFactory = null)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream? languages = assembly
                .GetManifestResourceStream("Microsoft.DevSkim.AI.resources.languages.json");
            Stream? comments = assembly
                .GetManifestResourceStream("Microsoft.DevSkim.AI.resources.comments.json");
            return new Languages(null, comments, languages);
        }
    }
}

