// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.DevSkim.LanguageProtoInterop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace Microsoft.DevSkim.VisualStudio
{
    /// <summary>
    /// VS lsp doesn't automatically send settings from the settings panel, so we need to manually refresh them on change
    /// </summary>
    public class SettingsChangedNotifier
    {
        public record DevSkimSetLanguageServerSettingsParams : IRequest
        {
            public PortableScannerSettings ScannerSettings { get; set; }
        }

        private readonly JsonRpc rpc;

        public SettingsChangedNotifier(JsonRpc rpc)
        {
            this.rpc = rpc;
        }

        public async Task SendSettingsChangedNotificationAsync(PortableScannerSettings settings)
        {
            await rpc.NotifyWithParameterObjectAsync(DevSkimMessages.SetServerSettings, new DevSkimSetLanguageServerSettingsParams() { ScannerSettings = settings });
        }
    }
}