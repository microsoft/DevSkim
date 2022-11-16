using DevSkim.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Server;

var server = await LanguageServer.From(options =>
    options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .WithHandler<TextDocumentSyncHandler>()
);

await server.WaitForExit;