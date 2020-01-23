// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

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
        private OptionsDialogPageControl optionsDialogControl;
        private Settings _settings;

        protected override UIElement Child
        {
            get { return optionsDialogControl ?? (optionsDialogControl = new OptionsDialogPageControl()); }
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            if (_settings == null)            
                _settings = Settings.GetSettings();

            optionsDialogControl.UseDefaultRules.IsChecked  = _settings.UseDefaultRules;
            optionsDialogControl.UseCustomRules.IsChecked   = _settings.UseCustomRules;
            optionsDialogControl.CustomRulesPath.Text       = _settings.CustomRulesPath;

            optionsDialogControl.EnableImportantRules.IsChecked = _settings.EnableImportantRules;
            optionsDialogControl.EnableModerateRules.IsChecked = _settings.EnableModerateRules;

            optionsDialogControl.EnableBestPracticeRules.IsChecked = _settings.EnableBestPracticeRules;
            optionsDialogControl.EnableManualReviewRules.IsChecked = _settings.EnableManualReviewRules;

            optionsDialogControl.SuppressDays.Text          = _settings.SuppressDays.ToString();
        }

        protected override void OnDeactivate(CancelEventArgs e)
        {
            UpdateSettings();

            base.OnDeactivate(e);
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

        /// <summary>
        /// Transform data from UI page to settings store
        /// </summary>
        private void UpdateSettings()
        {
            _settings.UseDefaultRules   = (bool)optionsDialogControl.UseDefaultRules.IsChecked;
            _settings.UseCustomRules    = (bool)optionsDialogControl.UseCustomRules.IsChecked;

            _settings.EnableImportantRules = (bool)optionsDialogControl.EnableImportantRules.IsChecked;
            _settings.EnableModerateRules = (bool)optionsDialogControl.EnableModerateRules.IsChecked;

            _settings.EnableBestPracticeRules = (bool)optionsDialogControl.EnableBestPracticeRules.IsChecked;
            _settings.EnableManualReviewRules = (bool)optionsDialogControl.EnableManualReviewRules.IsChecked;

            _settings.CustomRulesPath   = optionsDialogControl.CustomRulesPath.Text;

            int val;
            if (int.TryParse(optionsDialogControl.SuppressDays.Text, out val) && val > 0)
                _settings.SuppressDays = val;
        }

    }
}