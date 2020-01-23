#README

This is a language server implementation of DevSkim - an out of proc engine that handles the analysis capabilities for this VS Code plugin.  The client component (in ../client) handles the actual integration with VS Code and runs in process.

A primer for VS Code Lanuage Servers can be found at <https://code.visualstudio.com/docs/extensions/example-language-server> and a primer on the protocol between the language server and client can be found at <https://github.com/Microsoft/language-server-protocol/blob/master/protocol.md>.

As with most VS Code extensions, this project is implemented in TypeScript running on Node.js.  

Before beginning development on the server component open a console window to this directory and type

	npm install

this will install the necessary dependencies from NPM

The README.md in the root directory of this project contains the bulk of details relevant to working with the DevSkim VS Code plugin

## Project files
Within this project the relevant files are:
* **package.json** - a vs code package definition, but very similar to the node.js format.  This is where NPM dependencies are listed. also contains instructions to copy build output to ../client/server so the client can use it
* **src/devskimServer.ts** - contains the main VS Code language server functionality, i.e. responding to connection events, response handlers, etc.  This file orchestrates most of the activity
* **src/devskimWorker.ts** - contains the class that handles all of the analysis logic
* **src/devskimObjects.ts** - contains interface, enum, and other definitions used by Devskim. 
* **src/regexHelpers.ts** - the DevSkim rules are implemented using python style regular expressions which are *mostly* like javascript regexes.  This file contains the logic to rationalize the differences
* **.vscode/launch.json** - instructions to VS Code on what actions to take in order to debug this project
* **.vscode/tasks.json** - build task definition

If you are looking to just add/edit the rules, they are loaded as a git submodule in ../client/rules 

## Debugging
Have the client and server open in separate VS Code instances, and build both (CTRL+Shift+b).  Hit "F5" in both instancs of code to get debugging running for both.  The server component automatically attaches after the client project has spawned a VS Code extension host window, however the server times out after 10 seconds if there is nothing to attach to.




