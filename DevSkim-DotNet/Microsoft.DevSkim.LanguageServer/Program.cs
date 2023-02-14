using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using System.Diagnostics;

namespace DevSkim.LanguageServer;

internal class Program
{
    public static void Main(string[] args)
    {
        MainAsync(args).Wait();
    }

    private static async Task MainAsync(string[] args)
    {
    #if DEBUG
        Debugger.Launch();
        while (!Debugger.IsAttached)
        {
            await Task.Delay(100);
        }

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
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
            options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .ConfigureLogging(
                        x => x
                            .AddSerilog(Log.Logger)
                            .AddLanguageProtocolLogging()
                    )
                    .WithHandler<TextDocumentSyncHandler>()
                    .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug)))
                    .WithServices(
                        services =>
                        {
                            services.AddSingleton(
                                new ConfigurationItem
                                {
                                    Section = "typescript",
                                }
                            ).AddSingleton(
                                new ConfigurationItem
                                {
                                    Section = "terminal",
                                }
                            );
                        }
                    )
                    .OnInitialize(
                        async (server, request, token) =>
                        {
                            Log.Logger.Debug("Server is starting...");
                            var manager = server.WorkDoneManager.For(
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
                            using var manager = await languageServer.WorkDoneManager.Create(
                                new WorkDoneProgressBegin { Title = "Beginning server routines..." }).ConfigureAwait(false);
                            
                            var configuration = await languageServer.Configuration.GetConfiguration(
                                new ConfigurationItem
                                {
                                    Section = "typescript",
                                }, new ConfigurationItem
                                {
                                    Section = "terminal",
                                }
                            ).ConfigureAwait(false);

                            var baseConfig = new JObject();
                            foreach (var config in languageServer.Configuration.AsEnumerable())
                            {
                                baseConfig.Add(config.Key, config.Value);
                            }

                            Log.Logger.Debug("Base Config: {@Config}", baseConfig);

                            var scopedConfig = new JObject();
                            foreach (var config in configuration.AsEnumerable())
                            {
                                scopedConfig.Add(config.Key, config.Value);
                            }

                            Log.Logger.Debug("Scoped Config: {@Config}", scopedConfig);
                            Log.Logger.Debug("Listening for client events...");
                        }
                    )
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
}