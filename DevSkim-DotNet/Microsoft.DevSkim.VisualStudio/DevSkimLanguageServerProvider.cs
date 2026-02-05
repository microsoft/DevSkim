// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.VisualStudio;

using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection;
using global::Microsoft.VisualStudio.Extensibility;
using global::Microsoft.VisualStudio.Extensibility.Editor;
using global::Microsoft.VisualStudio.Extensibility.LanguageServer;
using global::Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
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

    private static string GetLanguageServerPath()
    {
        var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(extensionDirectory))
        {
            return string.Empty;
        }

        return Path.Combine(extensionDirectory, "Server", "Microsoft.DevSkim.LanguageServer.exe");
    }
}
#pragma warning restore VSEXTPREVIEW_LSP
