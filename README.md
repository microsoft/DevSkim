# DevSkim

DevSkim is a framework of IDE extensions and Language analyzers that provide inline security analysis 
in the dev environment as the developer writes code. It has a flexible rule model that supports multiple programming
languages. The goal is to give the developer notification as they are introducing a security
vulnerability in order to fix the issue at the point of introduction, and to help build awareness
for the developer.

## Releases

Platform specific binaries of the CLI are available on our GitHub [releases](https://github.com/microsoft/DevSkim/releases) page.

The C# library is available on [NuGet](https://www.nuget.org/packages/Microsoft.CST.DevSkim/).

If you have .NET Core installed already you can install the CLI with `dotnet tool install --global Microsoft.CST.DevSkim.CLI`

The Visual Studio extension is available in the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=MS-CST-E.MicrosoftDevSkim).

The VS Code extension is available in the [Visual Studio Code Marketplace](https://marketplace.visualstudio.com/items?itemName=MS-CST-E.vscode-devskim).

### Repository Structure

DevSkim and its plugins/extensions are currently being merged here into a single repository.

This repository contains DevSkim and its plugins each within their own folder. Issues and contributions are accepted here for all of these tools:

* DevSkim - CLI tool, plus common rules and guidance
* DevSkim-VisualStudio-Extension
* DevSkim-VSCode-Plugin

### Writing Rules

Please see [Writing Rules](https://github.com/Microsoft/DevSkim/wiki/Writing-Rules) for
instructions on how to author new rules.

### Reporting Issues

Please see [CONTRIBUTING](https://github.com/Microsoft/DevSkim/blob/master/CONTRIBUTING.md) for
information on reporting issues and contributing code.

