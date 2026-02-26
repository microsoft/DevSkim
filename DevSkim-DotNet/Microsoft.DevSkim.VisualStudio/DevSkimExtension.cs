// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.DevSkim.VisualStudio;

using global::Microsoft.Extensions.DependencyInjection;
using global::Microsoft.VisualStudio.Extensibility;

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
            version: new Version(ThisAssembly.AssemblyFileVersion),
            publisherName: "Microsoft DevLabs",
            displayName: "Microsoft DevSkim",
            description: "DevSkim is a highly configurable security linter with a default ruleset focused on common security related issues.")
        {
            MoreInfo = "https://github.com/Microsoft/DevSkim",
            Tags = ["linter", "linters", "coding", "security", "static analysis"],
        },
    };

    /// <inheritdoc />
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
    }
}
