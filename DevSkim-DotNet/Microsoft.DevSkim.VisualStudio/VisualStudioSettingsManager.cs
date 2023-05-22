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

    internal class VisualStudioSettingsManager
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
            return (GetValueResultEnumToValueResultEnum(_settingsManager.TryGetValue($"{_subsetName}.{propertyName}", out T val)), val);
        }

        private ValueResultEnum GetValueResultEnumToValueResultEnum(GetValueResult getValueResult) => getValueResult switch
        {
            GetValueResult.Success => ValueResultEnum.Success,
            GetValueResult.Missing => ValueResultEnum.Missing,
            GetValueResult.Corrupt => ValueResultEnum.Corrupt,
            GetValueResult.IncompatibleType => ValueResultEnum.IncompatibleType,
            GetValueResult.ObsoleteFormat => ValueResultEnum.ObsoleteFormat,
            GetValueResult.UnknownError => ValueResultEnum.UnknownError,
            _ => ValueResultEnum.UnknownError
        };

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

        ///// <summary>
        ///// See UpdateSettingsGenerator
        ///// </summary>
        ///// <param name="propertyName"></param>
        //partial void UpdateSettings(string propertyName);

        void UpdateSettings(string propertyName)
        {
            switch (propertyName)
            {
                case "EnableCriticalSeverityRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableCriticalSeverityRules = res.Item2;
                        }
                        break;
                    }
                case "EnableImportantSeverityRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableImportantSeverityRules = res.Item2;
                        }
                        break;
                    }
                case "EnableModerateSeverityRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableModerateSeverityRules = res.Item2;
                        }
                        break;
                    }
                case "EnableManualReviewSeverityRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableManualReviewSeverityRules = res.Item2;
                        }
                        break;
                    }
                case "EnableBestPracticeSeverityRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableBestPracticeSeverityRules = res.Item2;
                        }
                        break;
                    }
                case "EnableHighConfidenceRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableHighConfidenceRules = res.Item2;
                        }
                        break;
                    }
                case "EnableMediumConfidenceRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableMediumConfidenceRules = res.Item2;
                        }
                        break;
                    }
                case "EnableLowConfidenceRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.EnableLowConfidenceRules = res.Item2;
                        }
                        break;
                    }
                case "CustomRulesPaths":
                    {
                        var res = Get<List<string>>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.CustomRulesPaths = res.Item2 ?? new List<string>();
                        }
                        break;
                    }
                case "CustomLanguagesPath":
                    {
                        var res = Get<string>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.CustomLanguagesPath = res.Item2;
                        }
                        break;
                    }
                case "CustomCommentsPath":
                    {
                        var res = Get<string>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.CustomCommentsPath = res.Item2;
                        }
                        break;
                    }
                case "SuppressionDurationInDays":
                    {
                        var res = Get<int>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.SuppressionDurationInDays = res.Item2;
                        }
                        break;
                    }
                case "SuppressionCommentStyle":
                    {
                        var res = Get<CommentStylesEnum>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.SuppressionCommentStyle = res.Item2;
                        }
                        break;
                    }
                case "ManualReviewerName":
                    {
                        var res = Get<string>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.ManualReviewerName = res.Item2;
                        }
                        break;
                    }
                case "GuidanceBaseURL":
                    {
                        var res = Get<string>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.GuidanceBaseURL = res.Item2;
                        }
                        break;
                    }
                case "IgnoreFiles":
                    {
                        var res = Get<List<string>>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.IgnoreFiles = res.Item2 ?? new List<string>();
                        }
                        break;
                    }
                case "IgnoreRulesList":
                    {
                        var res = Get<List<string>>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.IgnoreRulesList = res.Item2 ?? new List<string>();
                        }
                        break;
                    }
                case "IgnoreDefaultRules":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.IgnoreDefaultRules = res.Item2;
                        }
                        break;
                    }
                case "RemoveFindingsOnClose":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.RemoveFindingsOnClose = res.Item2;
                        }
                        break;
                    }
                case "ScanOnOpen":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.ScanOnOpen = res.Item2;
                        }
                        break;
                    }
                case "ScanOnSave":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.ScanOnSave = res.Item2;
                        }
                        break;
                    }
                case "ScanOnChange":
                    {
                        var res = Get<bool>(propertyName);
                        if (res.Item1 == ValueResultEnum.Success)
                        {
                            _currentSettings.ScanOnChange = res.Item2;
                        }
                        break;
                    }
                default: break;
            }
        }
    }
}
