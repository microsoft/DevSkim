using DevSkim.LanguageServer;
using OmniSharp.Extensions.LanguageServer.Server;

var options = new LanguageServerOptions();
options = options.WithOutput(Console.OpenStandardOutput());
options = options.WithInput(Console.OpenStandardInput());
options = options.AddHandler(new TextDocumentSyncHandler());
LanguageServer.Create(options => { });