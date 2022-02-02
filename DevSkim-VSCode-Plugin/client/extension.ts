/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { ExtensionToCodeCommentStyle } from '../common/languagesAccess';

import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';
import { CodeFixMapping } from '../common/codeFixMapping';
import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind
} from 'vscode-languageclient/node';

import * as vscode from 'vscode';
import { DevSkimSettings, DevSkimSettingsObject } from '../common/devskimSettings';
import { getCodeFixMapping, getDevSkimPath, getDotNetPath, getSetSettings } from '../common/notificationNames';
import { selectors } from '../common/selectors';
import { DevSkimFixer } from './devSkimFixer';

let client: LanguageClient;
const serverPath = path.join('server','out', 'server', 'server.js');

async function resolveDotNetPath(): Promise<string> {
	const result = await vscode.commands.executeCommand<any>(
		"dotnet.acquire",
		{
			version: "6.0",
			requestingExtensionId: "MS-CST-E.vscode-devskim",
		}
	);
	return result?.dotnetPath;
}

function getDevSkimConfiguration(section='devskim' ): DevSkimSettings {
	const settings: DevSkimSettings = new DevSkimSettingsObject();
	settings.enableBestPracticeRules = vscode.workspace.getConfiguration(section).get('enableBestPracticeRules', false);
	settings.enableManualReviewRules = vscode.workspace.getConfiguration(section).get('enableManualReviewRules', false);
	settings.guidanceBaseURL = vscode.workspace.getConfiguration(section).get('guidanceBaseURL', "https://github.com/Microsoft/DevSkim/blob/main/guidance/");
	settings.ignoreFiles = vscode.workspace.getConfiguration(section).get('ignoreFiles',
		[ "out/.*", "bin/.*", "node_modules/.*", ".vscode/.*", "yarn.lock", "logs/.*", ".log", ".git" ]);
	settings.ignoreRulesList = vscode.workspace.getConfiguration(section).get('ignoreRulesList', "");
	settings.manualReviewerName = vscode.workspace.getConfiguration(section).get('manualReviewerName', '');
	settings.removeFindingsOnClose = vscode.workspace.getConfiguration(section).get('removeFindingsOnClose', false);
	settings.suppressionDurationInDays = vscode.workspace.getConfiguration(section).get('suppressionDurationInDays', 30);
	settings.suppressionCommentStyle = vscode.workspace.getConfiguration(section).get('suppressionCommentStyle', 'line');
	return settings;

}

export function activate(context: ExtensionContext) {
	const config = getDevSkimConfiguration();
	const fixer = new DevSkimFixer();
	fixer.setConfig(config);
	context.subscriptions.push(
		vscode.languages.registerCodeActionsProvider(selectors, fixer, {
			providedCodeActionKinds: DevSkimFixer.providedCodeActionKinds
		})
	);
	// The server bridge is implemented in node
	const serverModule = context.asAbsolutePath(
		serverPath
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
				documentSelector: selectors,
				synchronize: {
					// Notify the server about file changes to '.clientrc files contained in the workspace
					fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
				}
			};
	
			// Create the language client and start the client.
			client = new LanguageClient(
				'MS-CST-E.vscode-devskim',
				'DevSkim VS Code Client',
				serverOptions,
				clientOptions
			);
			client.onReady().then(() => {
				client.sendNotification(getSetSettings(),getDevSkimConfiguration());
				client.sendNotification(getDotNetPath(),dotNetPath);
				const devskimExtension = vscode.extensions.getExtension('MS-CST-E.vscode-devskim');
				if (!devskimExtension) {
					throw new Error('Could not find DevSkim extension.');
				}
				client.sendNotification(getDevSkimPath(),path.join(devskimExtension.extensionPath, 'devskimBinaries', 'devskim.dll'));
				client.onNotification(getCodeFixMapping(), (mapping: CodeFixMapping) => 
				{
					fixer.ensureMapHasMapping(mapping);
				});
				vscode.workspace.onDidChangeConfiguration(e => {
					const newConfig = getDevSkimConfiguration();
					client.sendNotification(getSetSettings(),newConfig);
					fixer.setConfig(newConfig);
				});
			});
			// Start the client. This will also launch the server
			client.start();
		}
	});
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
