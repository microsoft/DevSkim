namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using Microsoft.VisualStudio.Threading;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Static class used to pass settings from the GeneralOptionsPage to the VisualStudioConfigurationHandler
    /// </summary>
    internal static class StaticSettings
    {
        public static SettingsChangedNotifier SettingsNotifier { get; internal set; }
        internal static PortableScannerSettings portableSettings { get; set; } = new PortableScannerSettings();

        internal static void Push()
        {
            Task.Run(() => SettingsNotifier.SendSettingsChangedNotificationAsync(portableSettings)).Wait();
        }
    }
}
