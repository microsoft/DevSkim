// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DevSkim.LanguageServer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Server;
using OmnisharpLanguageServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace Microsoft.DevSkim.LanguageServer
{
    public class Server : IDisposable
    {
        public record CreationOptions();

        private readonly OmnisharpLanguageServer server;

        public Server(CreationOptions creationOptions, Action<LanguageServerOptions> onOptionsFunc)
        {
            server = OmnisharpLanguageServer.PreInit(options =>
            {
                options.Services.AddSingleton(server)
                    .AddSingleton<ILanguageServerFacade>(server);
                options
                    .WithHandler<DevSkimTextDocumentSyncHandler>();
                onOptionsFunc(options);
            });
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await server.Initialize(cancellationToken);

            server.LogInfo($"Running on processId {Environment.ProcessId}");

            //if (FeatureProvider.TracingEnabled)
            //{
            //    Trace.Listeners.Add(new ServerLogTraceListener(server));
            //}
            await server.WaitForExit;
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }
}
