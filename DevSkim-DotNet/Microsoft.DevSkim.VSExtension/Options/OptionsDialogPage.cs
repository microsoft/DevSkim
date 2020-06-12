// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace Microsoft.DevSkim.VSExtension
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    [Guid(GuidStrings.GuidPageGeneral)]
    public class OptionsDialogPage : UIElementDialogPage
    {
        protected override UIElement Child
        {
            get { return optionsDialogControl ?? (optionsDialogControl = new OptionsDialogPageControl()); }
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            if (_settings == null)
                _settings = Settings.GetSettings();

            optionsDialogControl.UseDefaultRules.IsChecked = _settings.UseDefaultRules;
            optionsDialogControl.UseCustomRules.IsChecked = _settings.UseCustomRules;

            optionsDialogControl.CustomRulesPath.Text = _settings.CustomRulesPath;

            optionsDialogControl.EnableImportantRules.IsChecked = _settings.EnableImportantRules;
            optionsDialogControl.EnableModerateRules.IsChecked = _settings.EnableModerateRules;

            optionsDialogControl.EnableBestPracticeRules.IsChecked = _settings.EnableBestPracticeRules;
            optionsDialogControl.EnableManualReviewRules.IsChecked = _settings.EnableManualReviewRules;

            optionsDialogControl.SuppressDays.Text = _settings.SuppressDays.ToString();

            optionsDialogControl.UsePreviousLineSuppression.IsChecked = _settings.UsePreviousLineSuppression;
            optionsDialogControl.UseBlockSuppression.IsChecked = _settings.UseBlockSuppression;
        }

        protected override void OnApply(PageApplyEventArgs args)
        {
            if (args.ApplyBehavior == ApplyKind.Apply)
            {
                UpdateSettings();
                _settings.Save();

                SkimShim.ApplySettings();
            }

            base.OnApply(args);
        }

        protected override void OnDeactivate(CancelEventArgs e)
        {
            UpdateSettings();

            base.OnDeactivate(e);
        }

        private Settings _settings;
        private OptionsDialogPageControl optionsDialogControl;

        /// <summary>
        ///     Transform data from UI page to settings store
        /// </summary>
        private void UpdateSettings()
        {
            _settings.UseDefaultRules = (bool)optionsDialogControl.UseDefaultRules.IsChecked;
            _settings.UseCustomRules = (bool)optionsDialogControl.UseCustomRules.IsChecked;

            _settings.EnableImportantRules = (bool)optionsDialogControl.EnableImportantRules.IsChecked;
            _settings.EnableModerateRules = (bool)optionsDialogControl.EnableModerateRules.IsChecked;

            _settings.EnableBestPracticeRules = (bool)optionsDialogControl.EnableBestPracticeRules.IsChecked;
            _settings.EnableManualReviewRules = (bool)optionsDialogControl.EnableManualReviewRules.IsChecked;

            _settings.CustomRulesPath = optionsDialogControl.CustomRulesPath.Text;

            _settings.UsePreviousLineSuppression = (bool)optionsDialogControl.UsePreviousLineSuppression.IsChecked;
            _settings.UseBlockSuppression = (bool)optionsDialogControl.UseBlockSuppression.IsChecked;

            int val;
            if (int.TryParse(optionsDialogControl.SuppressDays.Text, out val) && val > 0)
                _settings.SuppressDays = val;
        }
    }
}