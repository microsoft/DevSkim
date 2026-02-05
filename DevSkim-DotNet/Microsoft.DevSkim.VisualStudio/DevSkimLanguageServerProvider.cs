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
    /// <inheritdoc/>
    public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration => new(
        "%DevSkim.LanguageServerProvider.DisplayName%",
        [
            // Try using "text" base document type to activate for all text files
            // The language server itself determines which files it can analyze
            DocumentFilter.FromDocumentType("text"),
        ]);

    /// <inheritdoc/>
    public override Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken cancellationToken)
    {
        // Log to a file for debugging since Debug.WriteLine may not be visible
        var logPath = Path.Combine(Path.GetTempPath(), "devskim-vs-extension.log");
        File.AppendAllText(logPath, $"[{DateTime.Now}] CreateServerConnectionAsync called\n");
        
        var serverPath = GetLanguageServerPath();
        File.AppendAllText(logPath, $"[{DateTime.Now}] Server path: {serverPath}\n");
        File.AppendAllText(logPath, $"[{DateTime.Now}] Assembly location: {Assembly.GetExecutingAssembly().Location}\n");
        
        if (string.IsNullOrEmpty(serverPath) || !File.Exists(serverPath))
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: Language server not found at: {serverPath}\n");
            Debug.WriteLine($"DevSkim: Language server not found at: {serverPath}");
            return Task.FromResult<IDuplexPipe?>(null);
        }

        File.AppendAllText(logPath, $"[{DateTime.Now}] Server exists, starting process...\n");
        
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

#pragma warning disable CA2000 // The process is disposed after Visual Studio sends the stop command.
        var process = new Process { StartInfo = startInfo };
#pragma warning restore CA2000

        // Log stderr for debugging
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Debug.WriteLine($"DevSkim Server: {e.Data}");
            }
        };

        if (process.Start())
        {
            process.BeginErrorReadLine();
            File.AppendAllText(logPath, $"[{DateTime.Now}] Language server started (PID: {process.Id})\n");
            Debug.WriteLine($"DevSkim: Language server started (PID: {process.Id})");

            return Task.FromResult<IDuplexPipe?>(new DuplexPipe(
                PipeReader.Create(process.StandardOutput.BaseStream),
                PipeWriter.Create(process.StandardInput.BaseStream)));
        }

        File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: Failed to start language server process\n");
        Debug.WriteLine("DevSkim: Failed to start language server process");
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
            Debug.WriteLine($"DevSkim: Language server initialization failed: {initializationFailureInfo?.StatusMessage}");
            // Disable the server from being activated again
            this.Enabled = false;
        }
        else
        {
            File.AppendAllText(logPath, $"[{DateTime.Now}] Language server initialized successfully\n");
            Debug.WriteLine("DevSkim: Language server initialized successfully");
        }

        return base.OnServerInitializationResultAsync(serverInitializationResult, initializationFailureInfo, cancellationToken);
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
