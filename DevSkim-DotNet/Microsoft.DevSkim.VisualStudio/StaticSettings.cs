namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using Microsoft.VisualStudio.Threading;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Static class used to pass settings from the GeneralOptionsPage to the VisualStudioConfigurationHandler
    /// </summary>
    internal static class StaticSettings
    {
        public static SettingsChangedNotifier SettingsNotifier { get; internal set; }
        internal static PortableScannerSettings portableSettings { get; set; } = new PortableScannerSettings();

        private static CancellationTokenSource tokenSource = new CancellationTokenSource();
        private static bool isWritingSettings = false;
        private static object lockObj = new object();

        internal static void Push()
        {
            if (isWritingSettings)
            {
                tokenSource.Cancel();
                tokenSource = new CancellationTokenSource();
            }
            var tokenToUse = tokenSource.Token;
            isWritingSettings = true;
            _ = Task.Run(async () =>
            {
                // Add a sleep before sending settings to collate multiple quick changes at once
                //  On startup the Set method for every property on the options page will be called
                //  which will call Push for every setting in the GeneralOptionsPage,
                //  this should reduce that to one notification instead.
                Thread.Sleep(1000);
                if (!tokenToUse.IsCancellationRequested)
                {
                    await SettingsNotifier.SendSettingsChangedNotificationAsync(portableSettings);
                }
                isWritingSettings = false;
            }
            , cancellationToken: tokenToUse);
        }
    }
}