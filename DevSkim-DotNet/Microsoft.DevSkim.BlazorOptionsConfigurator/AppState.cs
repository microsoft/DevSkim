using CommandLine;
using Microsoft.CST.OAT.Utils;

namespace Microsoft.DevSkim.BlazorOptionsConfigurator
{
    using Microsoft.DevSkim.CLI.Options;
    public static class AppState
    {
        public static SerializedAnalyzeCommandOptions commandOptions = CreateDefaultCommandOptions();

        public static SerializedAnalyzeCommandOptions CreateDefaultCommandOptions()
        {
            var opts = new SerializedAnalyzeCommandOptions();
            foreach (var property in typeof(SerializedAnalyzeCommandOptions).GetProperties())
            {
                var attrs = property.GetCustomAttributes(true).OfType<OptionAttribute>().FirstOrDefault();
                if (attrs is not null && attrs.LongName != "source-code")
                {
                    Helpers.SetValueByPropertyOrFieldName(opts, property.Name, attrs.Default);
                }
            }

            return opts;
        }
    }
}
