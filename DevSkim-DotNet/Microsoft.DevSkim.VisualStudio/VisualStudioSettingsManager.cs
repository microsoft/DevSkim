namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft;
    using System;
    using System.Collections.Generic;
    using Microsoft.DevSkim.LanguageProtoInterop;
    using Microsoft.DevSkim.VisualStudio.Options;
    using System.Linq;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsot.DevSkim.LanguageClient;
    using System.ComponentModel.Composition;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    internal partial class VisualStudioSettingsManager
    {
        private SettingsChangedNotifier _notifier;
        private DevSkimLanguageClient _client;
        private ISettingsManager _settingsManager;

        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        private PortableScannerSettings _currentSettings = new PortableScannerSettings();
        private string _subsetName = typeof(GeneralOptionsPage).FullName;
        public VisualStudioSettingsManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, DevSkimLanguageClient client)
        {
            _client = client;
            _settingsManager = serviceProvider.GetService(typeof(SVsSettingsPersistenceManager)) as ISettingsManager;
            Assumes.Present(_settingsManager);
            ISettingsSubset setting = _settingsManager.GetSubset($"{_subsetName}.*");
            setting.SettingChangedAsync += (sender, args) => UpdateSettingsTaskAsync(args.PropertyName.Substring(_subsetName.Length+1));
        }
        
        /// <summary>
        /// Gets the specified <paramref name="propertyName"/> of the <see cref="_subsetName"/> from the <see cref="_settingsManager"/>
        /// This is called by the generated code for <see cref="UpdateSettings(string)"/>
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the parameter in <see cref="ISettingsManager"/></typeparam>
        /// <param name="propertyName">The name of the parameter</param>
        /// <returns>When successful, Success and the value, when unsuccessful, an enum other than success and undefined.</returns>
        private (ValueResultEnum, T) Get<T>(string propertyName)
        {
            return (ToValueResultEnum(_settingsManager.TryGetValue($"{_subsetName}.{propertyName}", out T val)), val);
        }

        private ValueResultEnum ToValueResultEnum(GetValueResult getValueResult)
        {
            return getValueResult switch
            {
                GetValueResult.Success => ValueResultEnum.Success,
                GetValueResult.Missing => ValueResultEnum.Missing,
                GetValueResult.Corrupt => ValueResultEnum.Corrupt,
                GetValueResult.IncompatibleType => ValueResultEnum.IncompatibleType,
                GetValueResult.ObsoleteFormat => ValueResultEnum.ObsoleteFormat,
                GetValueResult.UnknownError => ValueResultEnum.UnknownError,
            };
        }

        private async Task PushSettingsToServerAsync()
        {
            await _client.SettingsNotifier?.SendSettingsChangedNotificationAsync(_currentSettings);
        }

        private async Task UpdateSettingsTaskAsync(string propertyName)
        {
            UpdateSettings(propertyName);
            await PushSettingsToServerAsync();
        }

        public async Task UpdateAllSettingsAsync()
        {
            foreach (string name in typeof(IDevSkimOptions).GetProperties().Select(x => x.Name))
            {
                UpdateSettings(name);
            }
            await PushSettingsToServerAsync();
        }

        /// <summary>
        /// See UpdateSettingsGenerator
        /// </summary>
        /// <param name="propertyName"></param>
        partial void UpdateSettings(string propertyName);
    }
}
