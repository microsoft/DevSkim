# DevSkim
[![Nuget CLI](https://img.shields.io/nuget/v/Microsoft.CST.DevSkim.CLI?label=CLI&logo=NuGet)](https://www.nuget.org/packages/Microsoft.CST.DevSkim.CLI)
[![Nuget Library](https://img.shields.io/nuget/v/Microsoft.CST.DevSkim?label=Library&logo=NuGet)](https://www.nuget.org/packages/Microsoft.CST.DevSkim)
[![Nuget CLI Installs](https://img.shields.io/nuget/dt/Microsoft.CST.DevSkim.CLI?logo=NuGet)](https://www.nuget.org/packages/Microsoft.CST.DevSkim.CLI)

DevSkim is a .NET library and cross-platform command line tool that performs static security analysis
of source code. It has a flexible rule model that supports multiple programming languages. The goal is
to identify security vulnerabilities as early as possible, fix them at the point of introduction, and
help build security awareness for developers.

### Features

* Built-in rules, and support for writing custom rules
* Cross-platform CLI built on .NET for analyzing files and directories
* C# library for embedding DevSkim analysis in your own tools
* SARIF, JSON, and text output formats
* Information and guidance provided for identified security issues
* Automated application of suggested fixes from SARIF results
* Optional suppression of unwanted findings
* Support for JSONPath, XPath and YmlPath based rules
* Broad language support including: C, C++, C#, Cobol, Go, Java, Javascript/Typescript, Python, and [more](https://github.com/Microsoft/DevSkim/wiki/Supported-Languages).

### Repository Structure

Issues and contributions are accepted here for:

* DevSkim Library
  * Location: `./DevSkim-DotNet/Microsoft.DevSkim/`
* DevSkim CLI
  * Location: `./DevSkim-DotNet/Microsoft.DevSkim.CLI/`
* Tests
  * Location: `./DevSkim-DotNet/Microsoft.DevSkim.Tests/`
* Default Rules and Guidance
  * Location: `./rules/default/` and `./guidance/`

## Official Releases

The C# library is available on NuGet as [Microsoft.CST.DevSkim](https://www.nuget.org/packages/Microsoft.CST.DevSkim/).

The .NET Global Tool is available on NuGet as [Microsoft.CST.DevSkim.CLI](https://www.nuget.org/packages/Microsoft.CST.DevSkim.CLI/).

Platform specific binaries of the DevSkim CLI are also available on our GitHub [releases page](https://github.com/microsoft/DevSkim/releases).

## Installation

### Library

Add the [Microsoft.CST.DevSkim](https://www.nuget.org/packages/Microsoft.CST.DevSkim/) package to your project:

`dotnet add package Microsoft.CST.DevSkim`

### Command Line Interface
#### .NET Global Tool (Recommended)

If you already have the .NET SDK installed, you can install the DevSkim CLI as a dotnet global tool by running the following from a command line:

`dotnet tool install --global Microsoft.CST.DevSkim.CLI`

This will add DevSkim to your PATH. You can then invoke `devskim` from a command line.

#### Self Contained App

Download the platform specific binary archive for your system (Windows, Mac OS, Linux) from the [releases page](https://github.com/microsoft/DevSkim/releases). Extract the archive, navigate to the DevSkim folder from a command line, and invoke `devskim` or `devskim.exe`.

#### Runtime Dependent App

First download and install the [Latest .NET runtime](https://dotnet.microsoft.com/).
Then download the DevSkim netcoreapp archive from the [releases page](https://github.com/microsoft/DevSkim/releases). Extract the archive, navigate to the DevSkim folder from a command line, and invoke `dotnet devskim.dll`.

## Build from Source

DevSkim requires the [.NET SDK](https://dotnet.microsoft.com/) (8.0 or later).

```bash
cd DevSkim-DotNet
dotnet build Microsoft.DevSkim.sln
dotnet test Microsoft.DevSkim.Tests/Microsoft.DevSkim.Tests.csproj
```

For more information, see the wiki page about how to [Build from Source](https://github.com/microsoft/DevSkim/wiki/Build-from-Source).

## Basic Usage

Analyze a directory and write the results as SARIF:

`devskim analyze --source-code /path/to/FilesToAnalyze --output-file results.sarif`

The CLI also provides the following commands:

* `fix` - Apply suggested fixes from a DevSkim SARIF file
* `suppress` - Add suppression comments for issues identified in a DevSkim SARIF file
* `verify` - Validate rule files

Run `devskim --help` or `devskim <command> --help` for all options. For more information, see the wiki page about the [Command Line Interface](https://github.com/microsoft/DevSkim/wiki/Command-Line-Interface).

## Writing Rules

Please see [Writing Rules](https://github.com/Microsoft/DevSkim/wiki/Writing-Rules) for
instructions on how to author rules.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

For more information, please see [How to Contribute](https://github.com/Microsoft/DevSkim/wiki/How-to-Contribute).

## Reporting Issues

For more information, please see [How to Contribute](https://github.com/Microsoft/DevSkim/wiki/How-to-Contribute).

### Reporting Security Vulnerabilities

To report a security vulnerability, please see [SECURITY.md](SECURITY.md).

## License

DevSkim is licensed under the [MIT license](LICENSE.txt).

## Privacy

Usage of this application is governed by the [Microsoft Privacy Statement](https://go.microsoft.com/fwlink/?LinkId=521839).