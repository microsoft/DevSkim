// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MediatR;
using Microsoft.DevSkim.LanguageProtoInterop;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DevSkim.LanguageServer
{
    [Method(DevSkimMessages.RescanDocument, Direction.ClientToServer)]
    public record RescanDocumentParams : IRequest
    {
        public DocumentUri? Uri { get; init; }
        public string? Text { get; init; }
        public int? Version { get; init; }
    }

    /// <summary>
    /// Handles requests from the client to rescan a document on demand
    /// </summary>
    public class RescanHandler : IJsonRpcRequestHandler<RescanDocumentParams>
    {
        private readonly ILogger<RescanHandler> _logger;
        private readonly TextDocumentSyncHandler _syncHandler;

        public RescanHandler(ILogger<RescanHandler> logger, TextDocumentSyncHandler syncHandler)
        {
            _logger = logger;
            _syncHandler = syncHandler;
        }

        async Task<Unit> IRequestHandler<RescanDocumentParams, Unit>.Handle(RescanDocumentParams request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"RescanHandler: Received rescan request for {request.Uri}");
            
            if (request.Uri is null || request.Text is null)
            {
                _logger.LogWarning("RescanHandler: Uri or Text is null");
                return Unit.Value;
            }

            _logger.LogDebug($"RescanHandler: Rescanning document {request.Uri}");
            await _syncHandler.ScanDocumentAsync(request.Text, request.Version, request.Uri);

            return Unit.Value;
        }
    }
}
