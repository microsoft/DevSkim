namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    // Use a Static class to hold settings because SettingsManager does not work
    internal static class StaticSettings
    {
        internal static PortableScannerSettings portableSettings { get; set; } = new PortableScannerSettings();
    }
}
