namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    [Guid(PageGuidString)]
    public class FindingOptionsPage : DialogPage
    {
        public const string PageGuidString = "c88696f6-dd46-380e-a706-14e73fd51564";

        // TODO: Do we even have a scan all files in workspace type of commmand here?
        [Category("Finding Options")]
        [DisplayName("Remove Findings On Close")]
        [Description("By default, when a source file is closed the findings remain in the 'Error List' window.  " +
            "Setting this value to true will cause findings to be removed from 'Error List' when the document is closed.  " +
            "Note, setting this to true will cause findings that are listed when invoking the 'Scan all files in workspace' " +
            "command to automatically clear away after a couple of minutes.")]
        public bool RemoveFindingsOnClose
        {
            get; set;
        } = true;
    }
}
