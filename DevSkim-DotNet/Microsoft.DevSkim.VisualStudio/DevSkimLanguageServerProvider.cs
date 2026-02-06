// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.VisualStudio;

using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.InteropServices;
using global::Microsoft.DevSkim.LanguageProtoInterop;
using global::Microsoft.VisualStudio.Extensibility;
using global::Microsoft.VisualStudio.Extensibility.Editor;
using global::Microsoft.VisualStudio.Extensibility.LanguageServer;
using global::Microsoft.VisualStudio.Extensibility.Settings;
using global::Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
using Microsoft.Win32.SafeHandles;
using Nerdbank.Streams;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;

/// <summary>
/// DevSkim Language Server Provider.
/// Activates the DevSkim language server for security analysis when supported files are opened.
/// </summary>
#pragma warning disable VSEXTPREVIEW_LSP // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable VSEXTPREVIEW_SETTINGS
[VisualStudioContribution]
internal class DevSkimLanguageServerProvider : LanguageServerProvider
{
    private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "devskim-vs-extension.log");

    private Process? _serverProcess;
    private SafeHandle? _jobHandle;
    private readonly List<IDisposable> _settingsSubscriptions = [];
    private bool _initialPushDone;
    private CancellationTokenSource? _restartDebounce;

    /// <inheritdoc/>
    public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration => new(
        "%DevSkim.LanguageServerProvider.DisplayName%",
        [
            DocumentFilter.FromDocumentType("text"),
        ]);

    /// <inheritdoc/>
    public override async Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken cancellationToken)
    {
        // Configure logging on each server start (file logging may have changed)
        ConfigureLogging(await ReadFileLoggingSettingAsync(cancellationToken));

        Log.Debug("CreateServerConnectionAsync called");

        // Kill any leftover process from a previous activation
        StopServerProcess();

        var serverPath = GetLanguageServerPath();
        Log.Debug("Server path: {ServerPath}", serverPath);

        if (string.IsNullOrEmpty(serverPath) || !File.Exists(serverPath))
        {
            Log.Error("Language server not found at: {ServerPath}", serverPath);
            return null;
        }

        // Read current settings and pass them as LSP initializationOptions
        // so the server applies them during the initialize handshake.
        var currentSettings = await ReadAllSettingsAsync(cancellationToken);
        LanguageServerOptions = new LanguageServerOptions
        {
            InitializationOptions = JToken.FromObject(currentSettings),
        };
        Log.Debug("InitializationOptions set with current settings");

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
                Log.Debug("Server stderr: {Data}", e.Data);
            }
        };

        if (process.Start())
        {
            _serverProcess = process;
            AssignProcessToJobObject(process);
            process.BeginErrorReadLine();
            Log.Information("Language server started (PID: {ProcessId})", process.Id);

            return new DuplexPipe(
                PipeReader.Create(process.StandardOutput.BaseStream),
                PipeWriter.Create(process.StandardInput.BaseStream));
        }

        Log.Error("Failed to start language server process");
        return null;
    }

    /// <inheritdoc/>
    public override async Task OnServerInitializationResultAsync(
        ServerInitializationResult serverInitializationResult,
        LanguageServerInitializationFailureInfo? initializationFailureInfo,
        CancellationToken cancellationToken)
    {
        if (serverInitializationResult == ServerInitializationResult.Failed)
        {
            Log.Error("Language server initialization failed: {Message}", initializationFailureInfo?.StatusMessage);
            Enabled = false;
        }
        else
        {
            Log.Information("Language server initialized successfully");
            SubscribeToSettingsChanges();
            // Delay enabling change detection so SubscribeAsync initial callbacks are ignored
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                _initialPushDone = true;
                Log.Debug("Settings change detection enabled");
            });
        }

        await base.OnServerInitializationResultAsync(serverInitializationResult, initializationFailureInfo, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            foreach (var sub in _settingsSubscriptions)
            {
                sub.Dispose();
            }
            _settingsSubscriptions.Clear();
            _restartDebounce?.Cancel();
            _restartDebounce?.Dispose();
            StopServerProcess();
            _jobHandle?.Dispose();
            _jobHandle = null;
        }

        base.Dispose(isDisposing);
    }

    #region Logging

    private static void ConfigureLogging(bool enableFileLogging)
    {
        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Debug(outputTemplate: "[DevSkim] {Message:lj}{NewLine}{Exception}");

#if DEBUG
        // Always log to file in debug builds
        config = config
            .WriteTo.File(LogFilePath, rollingInterval: RollingInterval.Day)
            .MinimumLevel.Verbose();
#else
        if (enableFileLogging)
        {
            config = config.WriteTo.File(LogFilePath, rollingInterval: RollingInterval.Day);
        }

        config = config.MinimumLevel.Debug();
#endif

        Log.Logger = config.CreateLogger();
    }

    private async Task<bool> ReadFileLoggingSettingAsync(CancellationToken cancellationToken)
    {
        try
        {
            return (await Extensibility.Settings()
                .ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableFileLogging, cancellationToken))
                .ValueOrDefault(false);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Settings

    /// <summary>
    /// Subscribes to all settings changes. On change, restarts the server so it picks up
    /// new settings via initializationOptions during the LSP handshake.
    /// </summary>
    private void SubscribeToSettingsChanges()
    {
        SubscribeSetting(DevSkimSettingDefinitions.EnableCriticalSeverityRules);
        SubscribeSetting(DevSkimSettingDefinitions.EnableImportantSeverityRules);
        SubscribeSetting(DevSkimSettingDefinitions.EnableModerateSeverityRules);
        SubscribeSetting(DevSkimSettingDefinitions.EnableManualReviewSeverityRules);
        SubscribeSetting(DevSkimSettingDefinitions.EnableBestPracticeSeverityRules);
        SubscribeSetting(DevSkimSettingDefinitions.EnableHighConfidenceRules);
        SubscribeSetting(DevSkimSettingDefinitions.EnableMediumConfidenceRules);
        SubscribeSetting(DevSkimSettingDefinitions.EnableLowConfidenceRules);
        SubscribeSetting(DevSkimSettingDefinitions.IgnoreDefaultRules);
        SubscribeSetting(DevSkimSettingDefinitions.ScanOnOpen);
        SubscribeSetting(DevSkimSettingDefinitions.ScanOnSave);
        SubscribeSetting(DevSkimSettingDefinitions.ScanOnChange);
        SubscribeSetting(DevSkimSettingDefinitions.RemoveFindingsOnClose);
        SubscribeStringSetting(DevSkimSettingDefinitions.CustomRulesPaths);
        SubscribeStringSetting(DevSkimSettingDefinitions.CustomLanguagesPath);
        SubscribeStringSetting(DevSkimSettingDefinitions.CustomCommentsPath);
        SubscribeStringSetting(DevSkimSettingDefinitions.GuidanceBaseURL);
        SubscribeStringSetting(DevSkimSettingDefinitions.SuppressionCommentStyle);
        SubscribeStringSetting(DevSkimSettingDefinitions.ManualReviewerName);
        SubscribeStringSetting(DevSkimSettingDefinitions.IgnoreRulesList);
        SubscribeStringSetting(DevSkimSettingDefinitions.IgnoreFiles);
        SubscribeIntSetting(DevSkimSettingDefinitions.SuppressionDurationInDays);
    }

    private void SubscribeSetting(Setting.Boolean setting)
    {
        _ = Task.Run(async () =>
        {
            var sub = await Extensibility.Settings().SubscribeAsync(
                setting,
                CancellationToken.None,
                changeHandler: _ => OnSettingChanged());
            _settingsSubscriptions.Add(sub);
        });
    }

    private void SubscribeStringSetting(Setting.String setting)
    {
        _ = Task.Run(async () =>
        {
            var sub = await Extensibility.Settings().SubscribeAsync(
                setting,
                CancellationToken.None,
                changeHandler: _ => OnSettingChanged());
            _settingsSubscriptions.Add(sub);
        });
    }

    private void SubscribeIntSetting(Setting.Integer setting)
    {
        _ = Task.Run(async () =>
        {
            var sub = await Extensibility.Settings().SubscribeAsync(
                setting,
                CancellationToken.None,
                changeHandler: _ => OnSettingChanged());
            _settingsSubscriptions.Add(sub);
        });
    }

    private void OnSettingChanged()
    {
        if (!_initialPushDone)
        {
            return;
        }

        // Debounce: cancel any pending restart and start a new 2-second timer.
        // This way rapid changes (e.g. toggling multiple settings) cause only one restart.
        _restartDebounce?.Cancel();
        _restartDebounce = new CancellationTokenSource();
        var token = _restartDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000, token);
                Log.Information("Settings changed, restarting server");
                Enabled = false;
                await Task.Delay(500, CancellationToken.None);
                Enabled = true;
            }
            catch (OperationCanceledException)
            {
                // Debounced — a newer change superseded this one
            }
        });
    }

    /// <summary>
    /// Reads all DevSkim settings from VS and maps them to a <see cref="PortableScannerSettings"/>.
    /// </summary>
    private async Task<PortableScannerSettings> ReadAllSettingsAsync(CancellationToken cancellationToken)
    {
        var api = Extensibility.Settings();

        var suppressionStyle = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.SuppressionCommentStyle, cancellationToken))
            .ValueOrDefault("Line");
        var parsedStyle = Enum.TryParse<CommentStylesEnum>(suppressionStyle, ignoreCase: true, out var style)
            ? style
            : CommentStylesEnum.Line;

        return new PortableScannerSettings
        {
            EnableCriticalSeverityRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableCriticalSeverityRules, cancellationToken)).ValueOrDefault(true),
            EnableImportantSeverityRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableImportantSeverityRules, cancellationToken)).ValueOrDefault(true),
            EnableModerateSeverityRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableModerateSeverityRules, cancellationToken)).ValueOrDefault(true),
            EnableManualReviewSeverityRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableManualReviewSeverityRules, cancellationToken)).ValueOrDefault(true),
            EnableBestPracticeSeverityRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableBestPracticeSeverityRules, cancellationToken)).ValueOrDefault(true),
            EnableHighConfidenceRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableHighConfidenceRules, cancellationToken)).ValueOrDefault(true),
            EnableMediumConfidenceRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableMediumConfidenceRules, cancellationToken)).ValueOrDefault(true),
            EnableLowConfidenceRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.EnableLowConfidenceRules, cancellationToken)).ValueOrDefault(false),
            IgnoreDefaultRules = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.IgnoreDefaultRules, cancellationToken)).ValueOrDefault(false),
            ScanOnOpen = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.ScanOnOpen, cancellationToken)).ValueOrDefault(true),
            ScanOnSave = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.ScanOnSave, cancellationToken)).ValueOrDefault(true),
            ScanOnChange = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.ScanOnChange, cancellationToken)).ValueOrDefault(true),
            RemoveFindingsOnClose = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.RemoveFindingsOnClose, cancellationToken)).ValueOrDefault(true),
            CustomRulesPathsString = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.CustomRulesPaths, cancellationToken)).ValueOrDefault(""),
            CustomLanguagesPath = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.CustomLanguagesPath, cancellationToken)).ValueOrDefault(""),
            CustomCommentsPath = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.CustomCommentsPath, cancellationToken)).ValueOrDefault(""),
            GuidanceBaseURL = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.GuidanceBaseURL, cancellationToken)).ValueOrDefault("https://github.com/microsoft/devskim/tree/main/guidance"),
            SuppressionCommentStyle = parsedStyle,
            ManualReviewerName = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.ManualReviewerName, cancellationToken)).ValueOrDefault(""),
            SuppressionDurationInDays = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.SuppressionDurationInDays, cancellationToken)).ValueOrDefault(30),
            IgnoreRulesListString = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.IgnoreRulesList, cancellationToken)).ValueOrDefault(""),
            IgnoreFilesString = (await api.ReadEffectiveValueAsync(DevSkimSettingDefinitions.IgnoreFiles, cancellationToken)).ValueOrDefault(""),
        };
    }

    #endregion

    #region Process Management

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

    #endregion
}
#pragma warning restore VSEXTPREVIEW_LSP
