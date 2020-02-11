//------------------------------------------------------------------------------
// <copyright file="VSPackage.cs" company="Microsoft">
//      Copyright(C) Microsoft.All rights reserved.
//      Licensed under the MIT License.See LICENSE.txt in the project root for license information.
// </copyright>
//------------------------------------------------------------------------------


using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// This class implements a Visual Studio package that is registered for the Visual Studio IDE.
    /// The package class uses a number of registration attributes to specify integration parameters.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(OptionsDialogPage), "DevSkim Options", "General", 100, 101, supportsAutomation: true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(GuidStrings.GuidPackage)]
    class VSPackage : AsyncPackage
    {
        /// <summary>
        /// Initialization of the package.  This is where you should put all initialization
        /// code that depends on VS services.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            base.Initialize();
            // TODO: add initialization code here

            // Initialize shared components
            DTE = await GetServiceAsync(typeof(DTE)) as DTE2;

            // Initialize ActivityLog
            _log = await GetServiceAsync(typeof(SVsActivityLog)) as IVsActivityLog;
        }

        /// <summary>
        /// Get active open solution
        /// </summary>
        /// <returns>Open solution</returns>
        public static Solution GetSolution()
        {
            if (DTE.Solution == null)
                return null;

            return DTE.Solution;
        }

        public static void LogEvent(string message)
        {
            if (_log == null) return;

            int hr = _log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
                "Microsoft.DevSkim.VSExtension",
                message);
        }

        /// <summary>
        /// Gets project name that contains provided file
        /// </summary>
        /// <param name="fileName">File that belongs to project</param>
        /// <returns>Project name</returns>
        public static string GetProjectName(string fileName)
        {
            string result = null;            
            if (DTE != null && DTE.Solution != null)
            {
                ProjectItem prj= DTE.Solution.FindProjectItem(fileName);
                if (prj != null)
                    result = prj.ContainingProject.Name;
            }

            return result;
        }

        private static DTE2 DTE { get; set; }
        private static IVsActivityLog _log;
    }
}
