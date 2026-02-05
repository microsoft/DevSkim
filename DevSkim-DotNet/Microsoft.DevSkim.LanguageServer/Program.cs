using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

namespace DevSkim.LanguageServer;

internal class Program
{
    public class Options
    {
    }

    static async Task Main(string[] args)
    {
#if DEBUG
        //while (!Debugger.IsAttached)
        //{
        //    await Task.Delay(100);
        //}
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File("devskim-server-log.txt", rollingInterval: RollingInterval.Day)
            .MinimumLevel.Verbose()
            .CreateLogger();
#else
        // Creates a "silent" logger
        Log.Logger = new LoggerConfiguration().CreateLogger();
#endif
        Options _options = new Options();
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                _options = o;
            });

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
                    .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug)))
                    .OnInitialize(
                        async (server, request, token) =>
                        {
                            Log.Logger.Debug("Server is starting...");
                            
                            // Initialize with default settings immediately
                            // This ensures rules are enabled even if the client
                            // doesn't push settings via workspace/configuration
                            StaticScannerSettings.UpdateWith(new Microsoft.DevSkim.LanguageProtoInterop.PortableScannerSettings());
                            Log.Logger.Debug("Default settings applied");
                            
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
