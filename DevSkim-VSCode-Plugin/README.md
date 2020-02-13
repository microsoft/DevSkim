# README

This is the VS Code plugin project for DevSkim.  It is implemented in two parts - a Client that handles the integration and interaction with VS Code, and an out of proc server that handles the analysis.  This allows for more process intensive analysis without interfering in the responsiveness of the IDE.  

A primer for VS Code Lanuage Servers can be found at <https://code.visualstudio.com/docs/extensions/example-language-server> and a primer on the protocol between the language server and client can be found at <https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md>.

As with most VS Code extensions, this project is implemented in TypeScript running on Node.js.

## PUBLIC PREVIEW

DevSkim is currently in *public preview*. We're looking forward to working with the community
to improve both the scanning engines and rules over the next few months, and welcome your feedback
and contributions!

## Running DevSkim in VS Code

For people simply interested in using DevSkim in VS Code, it can be installed and run from the [VS Code Extension Marketplace](https://marketplace.visualstudio.com/items?itemName=MS-DevSkim.vscode-devskim).  In VS Code launch the VS Code Quick Open (Ctrl + P), paste the folloiwng command, and press enter:

    ext install ms-devskim.vscode-devskim

This will install the DevSkim Plugin in

- **Windows:** %USERPROFILE%\.vscode\extensions\vscode-devskim
- **Mac/Linux:** $HOME/.vscode/extensions/vscode-devskim

The rules directory within the install location contains the JSON based rules definitions used by DevSkim to find and fix problems.  DevSkim will by default run any rules located in the rules/default (the rules that ship with DevSkim) and rules/custom (location for organizations to add their own rules) folders.  By default, only fairly high confidence, high severity rules are enabled, however the the VS Code Settings allow the user to configure VS Code to also run the rules for Low Severity, Defense-in-Depth, and Manual Review.

## Getting started with Development

Install the TypeScript compiler if you have not already done so.  Then clone this repo and:

    > cd client
    > npm install
    > code .

    > cd ../server
    > npm install
    > code .

This will install all of the dependencies and launch a VS Code instance for each component.  Once up and running hit "ctrl+shift+b" (command+shift+b on the Mac) in the server project to build the server.  The build script automatically copies the compiled server components into ../client/server, as the client needs a copy of server in order to function.  Switch to the client VS Code instance, build it as well, and launch it (F5).  This will run the DevSkim plugin in a new instance of VS Code set up to debug extensions

The README.md in both the client and server folders have more details on their specific component files.

Additionally, it is necessary to grab the rules folder from the [all up DevSkim Repo](https://github.com/Microsoft/DevSkim) and copy it into the client directory.  This directory contains all of the rules that DevSkim runs against the file it is analyzing.

## Contributing

The README.md for the [all up DevSkim Repo](https://github.com/Microsoft/DevSkim) has the general details for contributing to the DevSkim project.  This section is specific for the VS Code Plugin.  As a TypeScript/Nodejs based project, use of NPM modules is par for the course.  Since this project is distributed by Microsoft in the VS Code Marketplace and Microsoft has a policy requiring review of licenses of all third party components it distributes, every NPM Module added to VS Code needs to be reviewed internally by Microsoft before distribution in the Marketplace.  This will delay contributions that add a new NPM Module from appearing in the official distribution of this plugin, however a couple of things can speed up the process.  NPM with no dependencies or a small dependency tree are quicker to review (the whole dependency tree needs license review), and MIT (or similar licenses) require much less review than more restrictive licenses, or custom licenses.
