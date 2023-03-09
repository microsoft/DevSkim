using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using System.Diagnostics;
using System.IO.Pipes;
using YamlDotNet.Core;

namespace DevSkim.LanguageServer;

internal class Program
{
	public static void Main(string[] args)
	{
		MainAsync(args).Wait();
	}

	private static (Stream, Stream) GetStreams(bool usePipes)
	{
		if (usePipes)
		{
			var stdInPipeName = @"input";
            var stdOutPipeName = @"output";
			var readerPipe = new NamedPipeClientStream(stdInPipeName);
            var writerPipe = new NamedPipeClientStream(stdOutPipeName);
			return (readerPipe, writerPipe);
		}
		else
		{
			return (Console.OpenStandardInput(),  Console.OpenStandardOutput());
		}
	}

    public class Options
    {
        [Option('p', "use pipes", Required = false, HelpText = "If set, will use pipes, if not set will use stdin/stdout.")]
        public bool UsePipes { get; set; }
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
		Options _opts = new Options();
        CommandLine.Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(o =>
               {
				   _opts = o;
               });
        (Stream input, Stream output) = GetStreams(_opts.UsePipes);

		Log.Logger.Debug("Configuring server...");
		IObserver<WorkDoneProgressReport> workDone = null!;

		var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
			options =>
				options
					.WithInput(input)
					.WithOutput(output)
					.ConfigureLogging(
						x => x
							.AddSerilog(Log.Logger)
							.AddLanguageProtocolLogging()
					)
					.WithHandler<TextDocumentSyncHandler>()
					.WithHandler<DidChangeConfigurationHandler>()
					.WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug)))
					.WithConfigurationSection(ConfigHelpers.Section)
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
									Section = ConfigHelpers.Section
								}
							).ConfigureAwait(false);
							ConfigHelpers.SetScannerSettings(configuration);
							Log.Logger.Debug("Listening for client events...");
						}
					)
		).ConfigureAwait(false);

		await server.WaitForExit.ConfigureAwait(false);
	}
}
