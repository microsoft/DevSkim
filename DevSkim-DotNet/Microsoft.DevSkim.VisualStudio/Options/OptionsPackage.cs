namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    [ProvideBindingPath]
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(RuleOptionsPage), "DevSkim", "Rules", 1000, 1001, true)]
    [ProvideProfile(typeof(RuleOptionsPage), "DevSkim", "Rules", 1000, 1002, true, DescriptionResourceID = 1003)]
    [ProvideOptionPage(typeof(SuppressionOptionsPage), "DevSkim", "Suppressions", 1000, 1004, true)]
    [ProvideProfile(typeof(SuppressionOptionsPage), "DevSkim", "Suppressions", 1000, 1005, true, DescriptionResourceID = 1006)]
    [ProvideOptionPage(typeof(GuidanceOptionsPage), "DevSkim", "Guidance ", 1000, 1007, true)]
    [ProvideProfile(typeof(GuidanceOptionsPage), "DevSkim", "Guidance", 1000, 1008, true, DescriptionResourceID = 1009)]
    [ProvideOptionPage(typeof(IgnoreOptionsPage), "DevSkim", "Ignores", 1000, 1010, true)]
    [ProvideProfile(typeof(IgnoreOptionsPage), "DevSkim", "Ignores", 1000, 1011, true, DescriptionResourceID = 1012)]
    [ProvideOptionPage(typeof(FindingOptionsPage), "DevSkim", "Findings", 1000, 1013, true)]
    [ProvideProfile(typeof(FindingOptionsPage), "DevSkim", "Findings", 1000, 1014, true, DescriptionResourceID = 1015)]
    [ProvideOptionPage(typeof(TriggerOptionsPage), "DevSkim", "Triggers", 1000, 1016, true)]
    [ProvideProfile(typeof(TriggerOptionsPage), "DevSkim", "Triggers", 1000, 1017, true, DescriptionResourceID = 1018)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class OptionsPackage : AsyncPackage
    {
        /// <summary>
        /// OptionPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ef3feecc-7c99-42f5-aa32-95c3b0d389aa";

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPackage"/> class.
        /// </summary>
        public OptionsPackage()
        {
            // Initialization code
        }

        #region Package Members

        #endregion
    }
}
