/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';

import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind
} from 'vscode-languageclient/node';

import * as vscode from 'vscode';

let client: LanguageClient;

async function resolveDotNetPath(): Promise<string> {
	const result = await vscode.commands.executeCommand<any>(
		"dotnet.acquire",
		{
			version: "6.0",
			requestingExtensionId: "lsp-sample",
		}
	);
	return result?.dotnetPath;
}

export function activate(context: ExtensionContext) {
	// The server bridge is implemented in node
	const serverModule = context.asAbsolutePath(
		path.join('server', 'out', 'server.js')
	);
	// The debug options for the server
	// --inspect=6009: runs the server in Node's Inspector mode so VS Code can attach to the server for debugging
	resolveDotNetPath().then((dotNetPath) =>
	{
		if (dotNetPath == undefined || dotNetPath == null)
		{
			// Error Can't start Extension
		}
		else
		{
			const debugOptions = { execArgv: ['--nolazy', '--inspect=6009'] };
			// If the extension is launched in debug mode then the debug server options are used
			// Otherwise the run options are used
			const serverOptions: ServerOptions = {
				run: { module: serverModule, transport: TransportKind.ipc },
				debug: {
					module: serverModule,
					transport: TransportKind.ipc,
					options: debugOptions
				}
			};
	
			// Options to control the language client
			const clientOptions: LanguageClientOptions = {
				// Register the server for plain text documents
				documentSelector: [{ scheme: 'file', language: 'c' },
				{ scheme: 'file', language: 'clojure' },
				{ scheme: 'file', language: 'coffeescript' },
				{ scheme: 'file', language: 'cpp' },
				{ scheme: 'file', language: 'csharp' },
				{ scheme: 'file', language: 'fsharp' },
				{ scheme: 'file', language: 'go' },
				{ scheme: 'file', language: 'groovy' },
				{ scheme: 'file', language: 'jade' },
				{ scheme: 'file', language: 'java' },
				{ scheme: 'file', language: 'javascript' },
				{ scheme: 'file', language: 'javascriptreact' },
				{ scheme: 'file', language: 'lua' },
				{ scheme: 'file', language: 'objective-c' },
				{ scheme: 'file', language: 'perl' },
				{ scheme: 'file', language: 'perl6' },
				{ scheme: 'file', language: 'php' },
				{ scheme: 'file', language: 'plaintext' },
				{ scheme: 'file', language: 'powershell' },
				{ scheme: 'file', language: 'python' },
				{ scheme: 'file', language: 'r' },
				{ scheme: 'file', language: 'ruby' },
				{ scheme: 'file', language: 'rust' },
				{ scheme: 'file', language: 'shellscript' },
				{ scheme: 'file', language: 'sql' },
				{ scheme: 'file', language: 'swift' },
				{ scheme: 'file', language: 'typescript' },
				{ scheme: 'file', language: 'typescriptreact' },
				{ scheme: 'file', language: 'vb' },
				{ scheme: 'file', language: 'xml' },
				{ scheme: 'file', language: 'yaml' }],
				synchronize: {
					// Notify the server about file changes to '.clientrc files contained in the workspace
					fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
				}
			};
	
			// Create the language client and start the client.
			client = new LanguageClient(
				'languageServerExample',
				'Language Server Example',
				serverOptions,
				clientOptions
			);
	
			// Start the client. This will also launch the server
			client.start();
			client.onReady().then(() => {
				client.sendNotification("dotnetPath",dotNetPath);
				const sampleExtension = vscode.extensions.getExtension('vscode-samples.lsp-sample');
				if (!sampleExtension) {
					throw new Error('Could not find sample extension.');
				}
				client.sendNotification("devskimPath",path.join(sampleExtension.extensionPath, 'devskimBinaries', 'devskim.dll'));
			});
		}
	});
	
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
