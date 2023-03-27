namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class RuleOptionPageGrid : DialogPage
    {
        [Category("Rule Options")]
        [DisplayName("Custom Rule Paths")]
        [Description("Paths to load cutom rules from")]
        public List<string> CustomRulePaths
        {
            get; set;
        } = new List<string>();
    }
}
