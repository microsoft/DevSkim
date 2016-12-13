# DevSkim

DevSkim is a framework of IDE plugins and Language analyzers that provide inline security analysis 
in the dev environment as the developer writes code. It is designed to work with multiple IDEs
(VS, VS Code, Sublime Text, etc.), and has a flexible rule model that supports multiple programming
languages. The idea is to give the developer notification as they are introducing a security
vulnerability in order to fix the issue at the point of introduction, and to help build awareness
for the developer.

### Repositories

DevSkim consists of multiple repositories (one for the rules, and one per plugin):

* [DevSkim](https://github.com/Microsoft/DevSkim/) - This repository, plus common rules and guidance
* [DevSkim-VisualStudio-Plugin](https://github.com/Microsoft/DevSkim-VisualStudio-Plugin/) - Visual Studio Plugin
* [DevSkim-Sublime-Plugin](https://github.com/Microsoft/DevSkim-Sublime-Plugin/) - Sublime Text Plugin
* [DevSkim-VSCode-Plugin](https://github.com/Microsoft/DevSkim-VSCode-Plugin/) - VS Code Plugin

Please access those projects to download the plugin, open issues, or contribute content.

### Writing Rules

Please see [Writing Rules](https://github.com/Microsoft/DevSkim/wiki/Writing-Rules) for
instructions on how to author new rules.

### Reporting Issues

Please see [CONTRIBUTING](https://github.com/Microsoft/DevSkim/blob/master/CONTRIBUTING.md) for
information on reporting issues and contributing code.

