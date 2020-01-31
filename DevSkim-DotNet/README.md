# DevSkim

DevSkim is a framework of IDE extensions and Language analyzers that provide inline security analysis 
in the dev environment as the developer writes code. It is designed to work with multiple IDEs
(VS, VS Code, Sublime Text, etc.), and has a flexible rule model that supports multiple programming
languages. The idea is to give the developer notification as they are introducing a security
vulnerability in order to fix the issue at the point of introduction, and to help build awareness
for the developer.

### PUBLIC PREVIEW

DevSkim is currently in *public preview*. We're looking forward to working with the community
to improve both the scanning engines and rules over the next few months, and welcome your feedback
and contributions!

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

