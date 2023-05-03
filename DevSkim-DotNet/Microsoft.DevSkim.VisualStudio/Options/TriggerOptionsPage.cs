namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.ComponentModel;

    public class TriggerOptionsPage : DialogPage
    {
        [Category("Trigger Options")]
        [DisplayName("Scan On Open")]
        [Description("Scan files on open.")]
        public bool ScanOnOpen
        {
            get; set;
        } = true;

        [Category("Trigger Options")]
        [DisplayName("Scan On Save")]
        [Description("Scan files on save.")]
        public bool ScanOnSave
        {
            get; set;
        } = true;

        [Category("Trigger Options")]
        [DisplayName("Scan On Change")]
        [Description("Scan files on change.")]
        public bool ScanOnChange
        {
            get; set;
        } = true;
    }
}
