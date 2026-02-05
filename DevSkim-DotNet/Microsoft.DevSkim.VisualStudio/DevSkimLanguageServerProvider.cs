// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.VisualStudio;

using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.InteropServices;
using global::Microsoft.VisualStudio.Extensibility;
using global::Microsoft.VisualStudio.Extensibility.Editor;
using global::Microsoft.VisualStudio.Extensibility.LanguageServer;
using global::Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
using Microsoft.Win32.SafeHandles;
using Nerdbank.Streams;

/// <summary>
/// DevSkim Language Server Provider.
/// Activates the DevSkim language server for security analysis when supported files are opened.
/// </summary>
#pragma warning disable VSEXTPREVIEW_LSP // Type is for evaluation purposes only and is subject to change or removal in future updates.
[VisualStudioContribution]
internal class DevSkimLanguageServerProvider : LanguageServerProvider
{
    private Process? _serverProcess;
    private SafeHandle? _jobHandle;

    /// <inheritdoc/>
    public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration => new(
        "%DevSkim.LanguageServerProvider.DisplayName%",
        [
            DocumentFilter.FromDocumentType("text"),
        ]);

    /// <inheritdoc/>
    public override Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(Path.GetTempPath(), "devskim-vs-extension.log");
        File.AppendAllText(logPath, $"[{DateTime.Now}] CreateServerConnectionAsync called\n");
        
        // Kill any leftover process from a previous activation
        StopServerProcess();
        
        var serverPath = GetLanguageServerPath();
        File.AppendAllText(logPath, $"[{DateTime.Now}] Server path: {serverPath}\n");
        
        if (string.IsNullOrEmpty(serverPath) || !File.Exists(serverPath))
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: Language server not found at: {serverPath}\n");
            return Task.FromResult<IDuplexPipe?>(null);
        }

        // Create a Windows Job Object so the server process is killed when the host process exits
        EnsureJobObject();

        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(serverPath),
        };

        var process = new Process { StartInfo = startInfo };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Debug.WriteLine($"DevSkim Server: {e.Data}");
            }
        };

        if (process.Start())
        {
            _serverProcess = process;
            AssignProcessToJobObject(process);
            process.BeginErrorReadLine();
            File.AppendAllText(logPath, $"[{DateTime.Now}] Language server started (PID: {process.Id})\n");

            return Task.FromResult<IDuplexPipe?>(new DuplexPipe(
                PipeReader.Create(process.StandardOutput.BaseStream),
                PipeWriter.Create(process.StandardInput.BaseStream)));
        }

        File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: Failed to start language server process\n");
        return Task.FromResult<IDuplexPipe?>(null);
    }

    /// <inheritdoc/>
    public override Task OnServerInitializationResultAsync(
        ServerInitializationResult serverInitializationResult,
        LanguageServerInitializationFailureInfo? initializationFailureInfo,
        CancellationToken cancellationToken)
    {
        var logPath = Path.Combine(Path.GetTempPath(), "devskim-vs-extension.log");
        
        if (serverInitializationResult == ServerInitializationResult.Failed)
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: Language server initialization failed: {initializationFailureInfo?.StatusMessage}\n");
            this.Enabled = false;
        }
        else
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] Language server initialized successfully\n");
        }

        return base.OnServerInitializationResultAsync(serverInitializationResult, initializationFailureInfo, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            StopServerProcess();
            _jobHandle?.Dispose();
            _jobHandle = null;
        }

        base.Dispose(isDisposing);
    }

    private void StopServerProcess()
    {
        try
        {
            if (_serverProcess is { HasExited: false })
            {
                _serverProcess.Kill();
                _serverProcess.Dispose();
            }
        }
        catch
        {
            // Best effort cleanup
        }
        finally
        {
            _serverProcess = null;
        }
    }

    private void EnsureJobObject()
    {
        if (_jobHandle != null)
        {
            return;
        }

        try
        {
            string jobName = "DevSkimLanguageServer" + Environment.ProcessId;
            _jobHandle = NativeMethods.CreateJobObject(IntPtr.Zero, jobName);

            var extendedInfo = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = 0x2000 /* JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE */
                }
            };

            int length = Marshal.SizeOf(extendedInfo);
            IntPtr pExtendedInfo = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(extendedInfo, pExtendedInfo, false);
                NativeMethods.SetInformationJobObject(_jobHandle, 9 /* JobObjectExtendedLimitInformation */, pExtendedInfo, (uint)length);
            }
            finally
            {
                Marshal.FreeHGlobal(pExtendedInfo);
            }
        }
        catch
        {
            // Non-fatal: process cleanup will fall back to Dispose/Kill
        }
    }

    private void AssignProcessToJobObject(Process process)
    {
        try
        {
            if (_jobHandle != null && !_jobHandle.IsInvalid)
            {
                NativeMethods.AssignProcessToJobObject(_jobHandle, process.Handle);
            }
        }
        catch
        {
            // Non-fatal
        }
    }

    private static string GetLanguageServerPath()
    {
        var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(extensionDirectory))
        {
            return string.Empty;
        }

        return Path.Combine(extensionDirectory, "Server", "Microsoft.DevSkim.LanguageServer.exe");
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern SafeWaitHandle CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll")]
        public static extern bool SetInformationJobObject(SafeHandle hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll")]
        public static extern bool AssignProcessToJobObject(SafeHandle hJob, IntPtr hProcess);

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
#pragma warning restore VSEXTPREVIEW_LSP
