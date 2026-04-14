using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

namespace DevSkim.LanguageServer;

internal class Program
{
    static async Task Main(string[] args)
    {
#if DEBUG
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File("devskim-server-log.txt", rollingInterval: RollingInterval.Day)
            .MinimumLevel.Verbose()
            .CreateLogger();
#else
        // Creates a "silent" logger
        Log.Logger = new LoggerConfiguration().CreateLogger();
#endif

        Log.Logger.Debug("Configuring server...");
        IObserver<WorkDoneProgressReport> workDone = null!;
        OmniSharp.Extensions.LanguageServer.Server.LanguageServer server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
            options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithServerInfo(new ServerInfo { Name = "DevSkim Language Server" })
                    .ConfigureLogging(
                        x => x
                            .AddSerilog(Log.Logger)
                            .AddLanguageProtocolLogging()
                    )
                    .WithHandler<TextDocumentSyncHandler>()
                    .WithHandler<CodeActionHandler>()
                    // Handle settings push from clients (devskim/setSettings custom method)
                    // This works for both VS Code and VS - avoids workspace/configuration issues
                    .WithHandler<VisualStudioConfigurationHandler>()
                    // Handle workspace/didChangeConfiguration from VS Code
                    // VS Code sends this notification; the handler pulls settings via workspace/configuration
                    .WithHandler<DidChangeConfigurationHandler>()
                    .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug)))
                    .OnInitialize(
                        async (server, request, token) =>
                        {
                            Log.Logger.Debug("Server is starting...");
                            
                            // Check if the client sent settings via initializationOptions
                            // (VS extension passes PortableScannerSettings here)
                            Microsoft.DevSkim.LanguageProtoInterop.PortableScannerSettings? clientSettings = null;
                            try
                            {
                                if (request.InitializationOptions is Newtonsoft.Json.Linq.JToken initOptions)
                                {
                                    clientSettings = initOptions.ToObject<Microsoft.DevSkim.LanguageProtoInterop.PortableScannerSettings>();
                                    Log.Logger.Debug("Received settings from initializationOptions: IgnoreDefaultRules={IgnoreDefaultRules}", clientSettings?.IgnoreDefaultRules);
                                }
                                else
                                {
                                    Log.Logger.Warning("No initializationOptions received (type: {Type}), using defaults", request.InitializationOptions?.GetType().Name ?? "null");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Warning(ex, "Failed to parse initializationOptions as settings");
                            }

                            // Apply client settings if provided, otherwise use defaults
                            var effectiveSettings = clientSettings ?? new Microsoft.DevSkim.LanguageProtoInterop.PortableScannerSettings();
                            StaticScannerSettings.UpdateWith(effectiveSettings);
                            Log.Logger.Debug("Settings applied: IgnoreDefaultRules={IgnoreDefaultRules}, RuleCount={RuleCount}", effectiveSettings.IgnoreDefaultRules, StaticScannerSettings.RuleSet.Count());
                            
                            OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone.IWorkDoneObserver manager = server.WorkDoneManager.For(
                                request, new WorkDoneProgressBegin
                                {
                                    Title = "Server is starting...",
                                }
                            );
                            workDone = manager;
                        }
                    )
                    .OnInitialized(
                        async (server, request, response, token) =>
                        {
                            Log.Logger.Debug("Server started");
                            workDone.OnNext(
                                new WorkDoneProgressReport
                                {
                                    Message = "Server started",
                                }
                            );
                            workDone.OnCompleted();
                        }
                    )
                    .OnStarted(
                        async (languageServer, token) =>
                        {
                            Log.Logger.Debug("Beginning server routines...");
                            using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone.IWorkDoneObserver manager = await languageServer.WorkDoneManager.Create(
                                new WorkDoneProgressBegin { Title = "Beginning server routines..." }).ConfigureAwait(false);

                            Log.Logger.Debug("Listening for client events...");
                        }
                    )
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
}
