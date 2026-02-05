// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.VisualStudio;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.VisualStudio.Extensibility;
using System.Security.Cryptography;

/// <summary>
/// Extension entry point for the DevSkim Visual Studio extension.
/// </summary>
[VisualStudioContribution]
internal class DevSkimExtension : Extension
{
    /// <inheritdoc/>
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "Microsoft.DevSkim.VisualStudio.f3a2c5e8-7d9b-4a1c-8e6f-2b3d4c5e6f7a",
            version: this.ExtensionAssemblyVersion,
            publisherName: "Microsoft DevLabs",
            displayName: "Microsoft DevSkim",
            description: "Security-focused static analysis tool for identifying vulnerabilities in source code."),
    };

    /// <inheritdoc />
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
    }
}
