namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.ComponentModel;

    public class OptionPageGrid : DialogPage
    {
        [Category("Scan Options")]
        [DisplayName("Scan On Save")]
        [Description("Scan When Document Is Saved")]
        public bool ScanOnSave
        {
            get; set;
        } = true;
        [Category("Scan Options")]
        [DisplayName("Scan On Change")]
        [Description("Scan When Document Is Changed")]
        public bool ScanOnChange
        {
            get; set;
        } = true;
        [Category("Scan Options")]
        [DisplayName("Scan On Open")]
        [Description("Scan When Document Is Opened")]
        public bool ScanOnOpen
        {
            get; set;
        } = true;


    }
}
