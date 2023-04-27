// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MediatR;
using Microsoft.DevSkim.LanguageProtoInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;

namespace DevSkim.LanguageServer
{
    [Method(DevSkimMessages.SetServerSettings, Direction.ClientToServer)]
    public record DevSkimSetLanguageServerSettingsRequest : IRequest
    {
        [JsonProperty("scannerSettings")]
        public JToken? ScannerSettings;
    }

    public record DevSkimSetLanguageServerSettingsRequestResult(PortableScannerSettings settings);

    /// <summary>
    /// Provides settings from the client to the server
    /// </summary>
    public class VisualStudioConfigurationHandler : IJsonRpcRequestHandler<DevSkimSetLanguageServerSettingsRequest>
    {
        public VisualStudioConfigurationHandler()
        {
        }

        public async Task<bool> Handle(DevSkimSetLanguageServerSettingsRequest request, CancellationToken cancellationToken)
        {
            var settings = request.ScannerSettings?.ToObject<PortableScannerSettings>();
            if (settings is not null)
            {
                await Task.Run(() => StaticScannerSettings.UpdateWith(settings), cancellationToken);
            }
            return true;
        }

        Task<Unit> IRequestHandler<DevSkimSetLanguageServerSettingsRequest, Unit>.Handle(DevSkimSetLanguageServerSettingsRequest request, CancellationToken cancellationToken)
        {
            var settings = request.ScannerSettings?.ToObject<PortableScannerSettings>();
            if (settings is not null)
            {
                StaticScannerSettings.UpdateWith(settings);
            }
            return Unit.Task;
        }
    }
}