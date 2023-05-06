namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft;
    using System;
    using System.Collections.Generic;
    using Microsoft.DevSkim.LanguageProtoInterop;
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
        private string _subsetName = "Microsoft.DevSkim.VisualStudio.GeneralOptionsPage";
        public VisualStudioSettingsManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, DevSkimLanguageClient client)
        {
            _client = client;
            _settingsManager = serviceProvider.GetService(typeof(SVsSettingsPersistenceManager)) as ISettingsManager;
            Assumes.Present(_settingsManager);
            IEnumerable<string> props = typeof(GeneralOptionsPage).GetProperties().Select(x => x.Name);
            foreach(string name in props)
            {
                UpdateSettings(name);
            }

            ISettingsSubset setting = _settingsManager.GetSubset($"{_subsetName}.*");
            setting.SettingChangedAsync += (sender, args) => UpdateSettingsTaskAsync(args.PropertyName.Substring(_subsetName.Length+1));
        }

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

        partial void UpdateSettings(string propertyName);
        // {
        //     switch (propertyName)
        //     {
        //         case "EnableCriticalSeverityRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableCriticalSeverityRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "EnableImportantSeverityRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableImportantSeverityRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "EnableModerateSeverityRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableModerateSeverityRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "EnableManualReviewSeverityRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableManualReviewSeverityRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "EnableBestPracticeSeverityRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableBestPracticeSeverityRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "EnableHighConfidenceRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableHighConfidenceRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "EnableMediumConfidenceRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableMediumConfidenceRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "EnableLowConfidenceRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.EnableLowConfidenceRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "CustomRulesPaths":
        //         {
        //             (GetValueResult, List<string>) res = Get<List<string>>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.CustomRulesPaths = res.Item2 ?? new List<string>();
        //             }
        //
        //             break;
        //         }
        //
        //         case "CustomLanguagesPath":
        //         {
        //             (GetValueResult, string) res = Get<string>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.CustomLanguagesPath = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "CustomCommentsPath":
        //         {
        //             (GetValueResult, string) res = Get<string>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.CustomCommentsPath = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "SuppressionDurationInDays":
        //         {
        //             (GetValueResult, int) res = Get<int>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.SuppressionDurationInDays = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "SuppressionCommentStyle":
        //         {
        //             (GetValueResult, CommentStylesEnum) res = Get<CommentStylesEnum>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.SuppressionCommentStyle = res.Item2.ToString();
        //             }
        //
        //             break;
        //         }
        //
        //         case "ManualReviewerName":
        //         {
        //             (GetValueResult, string) res = Get<string>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.ManualReviewerName = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "GuidanceBaseURL":
        //         {
        //             (GetValueResult, string) res = Get<string>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.GuidanceBaseURL = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "IgnoreRulesList":
        //         {
        //             (GetValueResult, List<string>) res = Get<List<string>>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.IgnoreRulesList = res.Item2 ?? new List<string>();
        //             }
        //
        //             break;
        //         }
        //
        //         case "IgnoreDefaultRules":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.IgnoreDefaultRules = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "RemoveFindingsOnClose":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.RemoveFindingsOnClose = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "ScanOnSave":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.ScanOnSave = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         case "ScanOnChange":
        //         {
        //             (GetValueResult, bool) res = Get<bool>(propertyName);
        //             if (res.Item1 == GetValueResult.Success)
        //             {
        //                 _currentSettings.ScanOnChange = res.Item2;
        //             }
        //
        //             break;
        //         }
        //
        //         default:
        //             break;
        //     }
        // }
    }
}
