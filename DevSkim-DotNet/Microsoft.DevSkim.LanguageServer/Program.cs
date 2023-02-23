using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using System.Diagnostics;
using System.Linq;

namespace DevSkim.LanguageServer;

internal class Program
{
    public static void Main(string[] args)
    {
        MainAsync(args).Wait();
    }

    private static DevSkimRuleProcessorOptions OptionsFromConfiguration(IConfiguration configuration)
    {
        var languagesPath = configuration.GetValue<string>("MS-CST-E.vscode-devskim.rules.customLanguagesPath");
        var commentsPath = configuration.GetValue<string>("MS-CST-E.vscode-devskim.rules.customCommentsPath");
        var severityFilter = Severity.Moderate | Severity.Critical | Severity.Important;
        if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim.rules.enableManualReviewRules"))
        {
            severityFilter |= Severity.ManualReview;
        }
        if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim.rules.enableBestPracticeRules"))
        {
            severityFilter |= Severity.BestPractice;
        }
        if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim.rules.enableUnspecifiedSeverityRules"))
        {
            severityFilter |= Severity.Unspecified;
        }
        var confidenceFilter = Confidence.Medium | Confidence.High;
        if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim.rules.enableUnspecifiedConfidenceRules"))
        {
            confidenceFilter |= Confidence.Unspecified;
        }
        if (configuration.GetValue<bool>("MS-CST-E.vscode-devskim.rules.enableLowConfidenceRules"))
        {
            confidenceFilter |= Confidence.Low;
        }
        return new DevSkimRuleProcessorOptions()
        {
            Languages = (string.IsNullOrEmpty(languagesPath) || string.IsNullOrEmpty(commentsPath)) ? DevSkimLanguages.LoadEmbedded() : DevSkimLanguages.FromFiles(commentsPath, languagesPath),
            SeverityFilter = severityFilter,
            ConfidenceFilter = confidenceFilter,
            LoggerFactory = NullLoggerFactory.Instance
        };
    }

    private static async Task MainAsync(string[] args)
    {
        #if DEBUG
            // Debugger.Launch();
            // while (!Debugger.IsAttached)
            // {
            //     await Task.Delay(100);
            // }

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

                            
                            IConfiguration configuration = await languageServer.Configuration.GetConfiguration(
                                new ConfigurationItem
                                {
                                    Section = "MS-CST-E.vscode-devskim"
                                }
                            ).ConfigureAwait(false);
                            StaticScannerSettings.RuleProcessorOptions = OptionsFromConfiguration(configuration);
                            StaticScannerSettings.IgnoreRuleIds = configuration.GetValue<string[]>("MS-CST-E.vscode-devskim.ignores.ignoreRuleList");
                            StaticScannerSettings.IgnoreFiles = configuration.GetValue<string[]>("MS-CST-E.vscode-devskim.ignores.ignoreFiles");
                            StaticScannerSettings.ScanOnOpen = configuration.GetValue<bool>("MS-CST-E.vscode-devskim.triggers.scanOnOpen");
                            StaticScannerSettings.ScanOnSave = configuration.GetValue<bool>("MS-CST-E.vscode-devskim.triggers.scanOnSave");
                            StaticScannerSettings.ScanOnChange = configuration.GetValue<bool>("MS-CST-E.vscode-devskim.triggers.scanOnChange");
                            Log.Logger.Debug("Listening for client events...");
                        }
                    )
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
}