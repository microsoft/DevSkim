// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace DevSkim.LanguageServer;

internal class DidChangeConfigurationHandler : DidChangeConfigurationHandlerBase
{
    private readonly ILogger<DidChangeConfigurationHandler> _logger;
    private readonly ILanguageServerConfiguration _configuration;

    /// <summary>
    /// Handle configuration changes from VS Code via workspace/didChangeConfiguration.
    /// When notified, pulls settings from the client and applies them.
    /// </summary>
    public DidChangeConfigurationHandler(ILogger<DidChangeConfigurationHandler> logger, ILanguageServerConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task<Unit> Handle(DidChangeConfigurationParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("DidChangeConfigurationHandler.cs: DidChangeConfigurationParams");
        ConfigHelpers.SetScannerSettings(
            (IConfiguration)await _configuration.GetConfiguration(new ConfigurationItem { Section = "MS-CST-E.vscode-devskim" })
        );
        return Unit.Value;
    }
}
