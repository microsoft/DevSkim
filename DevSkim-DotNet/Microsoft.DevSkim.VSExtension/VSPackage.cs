﻿// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    ///     This class implements a Visual Studio package that is registered for the Visual Studio IDE. The
    ///     package class uses a number of registration attributes to specify integration parameters.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(OptionsDialogPage), "DevSkim Options", "General", 100, 101, supportsAutomation: true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(GuidStrings.GuidPackage)]
    internal class VSPackage : AsyncPackage
    {
        /// <summary>
        ///     Gets project name that contains provided file
        /// </summary>
        /// <param name="fileName"> File that belongs to project </param>
        /// <returns> Project name </returns>
        public static string GetProjectName(string fileName)
        {
            string result = null;
            if (DTE != null && DTE.Solution != null)
            {
                ProjectItem prj = DTE.Solution.FindProjectItem(fileName);
                if (prj != null)
                    result = prj.ContainingProject.Name;
            }

            return result;
        }

        /// <summary>
        ///     Get active open solution
        /// </summary>
        /// <returns> Open solution </returns>
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
        ///     Initialization of the package. This is where you should put all initialization code that
        ///     depends on VS services.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { MaxDepth = 128 };
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            base.Initialize();
            // TODO: add initialization code here

            // Initialize shared components
            DTE = await GetServiceAsync(typeof(DTE)) as DTE2;

            // Initialize ActivityLog
            _log = await GetServiceAsync(typeof(SVsActivityLog)) as IVsActivityLog;
        }

        private static IVsActivityLog _log;
        private static DTE2 DTE { get; set; }
    }
}