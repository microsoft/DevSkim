namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    [Guid(PageGuidString)]
    public class TriggerOptionsPage : DialogPage
    {
        public const string PageGuidString = "5b1c0986-ea6e-3704-98af-3786cf4ef245";

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
