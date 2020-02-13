# README

This is a language client implementation of DevSkim. This is the in proc component of the plugin that handles integration with VS Code and spawns the out of proc server (located in ../server).  The server handles the actual analysis. 

A primer for VS Code Lanuage Client/Servers can be found at https://code.visualstudio.com/docs/extensions/example-language-server and a primer on the protocol between the language server and client can be found at https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md.

As with most VS Code extensions, this project is implemented in TypeScript running on Node.js.  

Before beginning development on the server component open a console window to this directory and type

	npm install

this will install the necessary dependencies from NPM

The README.md in the root directory of this project contains the bulk of details relevant to working with the DevSkim VS Code plugin

## Project files
Within this project the relevant files are:
* **package.json** - a vs code package definition, but very similar to the node.js format.  This is where NPM dependencies are listed. also contains instructions to copy build output to ../client/server so the client can use it.  It also informs VS Code of integration points.  Information on its format can be found at <https://code.visualstudio.com/docs/extensionAPI/extension-manifest> 
* **src/extensions.ts** - contains the main VS Code language client functionality.  The majority of the functionality is inherited from vscode-languageclient module, so the code in this file is relatively short.
* **.vscode/launch.json** - instructions to VS Code on what actions to take in order to debug this project
* **.vscode/tasks.json** - build task definition

Also of note, the ./server directory is populated by the output of the ../server build task.  the ./rules directory is a git submodule containing the rule definitions in a JSON format

## Debugging
Have the client and server open in separate VS Code instances, and build both (CTRL+Shift+b).  Hit "F5" in the client to launch an instance of VS Code configured to debug the extensions.  As most of the actual extension logic is in the server instead of the client, also hit F5 in the server instance of VS Code to attach and debug the server.
