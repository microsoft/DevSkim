DevSkim Extension for Visual Studio
===================================

The extension implements a security linter within Visual Studio, leveraging the rules from the [DevSkim](https://github.com/Microsoft/DevSkim) repository. It helps software engineers to write secure code by flagging potentially dangerous calls, and where possible, by giving in-context advice for remediation.

### PUBLIC PREVIEW

DevSkim is currently in *public preview*. We're looking forward to working with the community
to improve both the scanning engines and rules over the next few months, and welcome your feedback
and contributions!

![DevSkim Demo](https://github.com/Microsoft/DevSkim-VisualStudio-Extension/raw/master/media/DevSkim-VisualStudio-Demo-1.gif)

Installation
------------
Download the extension from the [Releases](https://github.com/Microsoft/DevSkim-VisualStudio-Extension/releases) page and install the `Microsoft.DevSkim.VSExtension.vsix` file. The extension will also be available in the [Visual Studio Marketplace](https://marketplace.visualstudio.com/vs) shortly.

Platform support
----------------

#### Operating System:

Microsoft Windows 7 and later

#### Visual Studio:

The extension is available for [Visual Studio 2015](https://www.visualstudio.com/vs/) and [Visual Studio 2017](https://www.visualstudio.com/vs/).

Rules System
------------

The extension supports both built-in and custom rules.

Built-in rules come from the [DevSkim](https://github.com/Microsoft/DevSkim) repository, and must be stored
in the `rules` directory within the `Microsoft.DevSkim.VSExtension` directory.

Rules are organized by subdirectory and file, but are flattened internally when loaded.

Each rule contains a set of patterns (strings and regular expressions) to match, a list of file types to
apply the rule to, and, optionally, a list of possible code fixes. 

For more information on rules format, see [Writing-Rules](https://github.com/Microsoft/DevSkim/wiki/Writing-Rules).


Reporting Issues
----------------

Please see [CONTRIBUTING](https://github.com/Microsoft/DevSkim-VisualStudio-Extension/blob/master/CONTRIBUTING.md) for information on reporting issues and contributing code.
