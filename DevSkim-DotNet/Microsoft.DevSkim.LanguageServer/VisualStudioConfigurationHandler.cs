// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MediatR;
using Microsoft.DevSkim.LanguageProtoInterop;
using OmniSharp.Extensions.JsonRpc;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System.Runtime.InteropServices;

namespace DevSkim.LanguageServer
{
    [Method(DevSkimMessages.SetServerSettings, Direction.ClientToServer)]
    public record DevSkimSetLanguageServerSettingsRequest : IRequest<bool>
    {
        public PortableScannerSettings ScannerSettings;
    }

    public record DevSkimSetLanguageServerSettingsRequestResult(PortableScannerSettings settings);

    /// <summary>
    /// Retrieves the recommended folder to place a new bicepconfig.json file (used by client)
    /// </summary>
    public class VisualStudioConfigurationHandler : IJsonRpcRequestHandler<DevSkimSetLanguageServerSettingsRequest, bool>
    {
        public VisualStudioConfigurationHandler()
        {
        }

        public async Task<bool> Handle(DevSkimSetLanguageServerSettingsRequest request, CancellationToken cancellationToken)
        {
            StaticScannerSettings.UpdateWith(request.ScannerSettings);
            return true;
        }
    }
}