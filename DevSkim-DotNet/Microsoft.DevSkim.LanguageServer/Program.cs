using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

namespace DevSkim.LanguageServer;

internal class Program
{
	public static void Main(string[] args)
	{
		MainAsync(args).Wait();
	}

    public class Options
	{
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
        (Stream input, Stream output) = (Console.OpenStandardInput(), Console.OpenStandardOutput());

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


							// This calls "workspace/configuration" over jsonrpc to the language client
							//		The Visual Studio client doesn't understand this, so temporarily commented out until it can listen and respond with the right configuration

							//IConfiguration configuration = await languageServer.Configuration.GetConfiguration(
							//	new ConfigurationItem
							//	{
							//		Section = ConfigHelpers.Section
							//	}
							//).ConfigureAwait(false);
							//ConfigHelpers.SetScannerSettings(configuration);
							Log.Logger.Debug("Listening for client events...");
						}
					)
		).ConfigureAwait(false);

		await server.WaitForExit.ConfigureAwait(false);
	}
}
