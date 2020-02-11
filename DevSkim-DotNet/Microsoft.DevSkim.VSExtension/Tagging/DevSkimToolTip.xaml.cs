// Copyright(C) Microsoft.All rights reserved.
// Licensed under the MIT License.See LICENSE.txt in the project root for license information.

using Microsoft.DevSkim.VSExtension.Resources;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// Interaction logic for DevSkimToolTip.xaml
    /// </summary>
    public partial class DevSkimToolTip : UserControl
    {
        const string url_preffix = "https://github.com/Microsoft/DevSkim/blob/master/guidance/";

        public DevSkimToolTip(Rule rule)
        {
            _rule = rule;
            InitializeComponent();
            System.Drawing.Color textColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolboxContentTextColorKey);
            System.Drawing.Color linkColor = VSColorTheme.GetThemedColor(ThemedDialogColors.HyperlinkColorKey);
            System.Drawing.Color subColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolboxContentTextColorKey);

            this.TitleBox.Text = rule.Name;

            this.MessageBox.Foreground = new SolidColorBrush(Color.FromRgb(textColor.R, textColor.G, textColor.B));                        
            this.MessageBox.Text = rule.Description;

            if (!string.IsNullOrEmpty(rule.Recommendation))
            {
                this.MessageBox.Text = string.Concat(this.MessageBox.Text, "\n", string.Format(Messages.FixGuidence, rule.Recommendation));
            }

            this.SeverityBox.Foreground = new SolidColorBrush(Color.FromRgb(subColor.R, subColor.G, subColor.B));
            this.SeverityBox.Text = string.Format(Messages.Severity,rule.Severity);            

            this.Url.Foreground = new SolidColorBrush(Color.FromRgb(linkColor.R, linkColor.G, linkColor.B));
            this.Url.NavigateUri = new Uri(url_preffix+rule.RuleInfo);            
        }

        private void Url_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            IVsWindowFrame ppFrame;
            IVsWebBrowsingService browserService;
            browserService = Package.GetGlobalService(typeof(SVsWebBrowsingService)) as IVsWebBrowsingService;

            browserService.Navigate(this.Url.NavigateUri.AbsoluteUri, 0, out ppFrame);

            VSPackage.LogEvent(string.Format("More info invoked on {0} {1}", _rule.Id, _rule.Name));
        }

        private Rule _rule;
    }
}
