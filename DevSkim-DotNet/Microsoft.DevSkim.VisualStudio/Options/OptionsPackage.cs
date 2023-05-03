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
    // TODO: [ProvideOptionPage(typeof(AllOptionsPage), "DevSkim", "All Options", 0, 0, true)]
    [ProvideOptionPage(typeof(RuleOptionsPage), "DevSkim", "Rules", 0, 0, true)]
    [ProvideOptionPage(typeof(SuppressionOptionsPage), "DevSkim", "Suppressions", 0, 0, true)]
    [ProvideOptionPage(typeof(GuidanceOptionsPage), "DevSkim", "Guidance ", 0, 0, true)]
    [ProvideOptionPage(typeof(IgnoreOptionsPage), "DevSkim", "Ignores", 0, 0, true)]
    [ProvideOptionPage(typeof(FindingOptionsPage), "DevSkim", "Findings", 0, 0, true)]
    [ProvideOptionPage(typeof(TriggerOptionsPage), "DevSkim", "Triggers", 0, 0, true)]
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
