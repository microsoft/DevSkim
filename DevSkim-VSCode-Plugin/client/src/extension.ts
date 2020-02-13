/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
'use strict';

import * as path from 'path';

import * as vscode from 'vscode';
import {
	LanguageClient,
	LanguageClientOptions,
	RequestType,
	ServerOptions,
	TextDocumentIdentifier,
	TextEdit,
	TransportKind,
} from 'vscode-languageclient';
import {DevSkimSettings, DevSkimSettingsObject} from "./devskim.settings";
import {getDocumentSelectors} from "./document-selectors";

let client: LanguageClient;

interface ValidateDocsParams {
	textDocuments: TextDocumentIdentifier[];
}

export class ValidateDocsRequest {
	public static type: RequestType<ValidateDocsParams,void,void,void>
		= new RequestType<ValidateDocsParams, void, void, void>('textDocument/devskim/validatedocuments');
}

export class ReloadRulesRequest {
	public static type: RequestType<{},void,void,void> = new RequestType<{}, void, void, void>('devskim/validaterules')
}

export function activate(context: vscode.ExtensionContext) {

	try {
		const serverModule = context.asAbsolutePath(path.join("server", "out", 'index.js'));
		console.log(`Server module: ${serverModule}`);

		// The debug options for the server
		const devSkimProperties = getDevSkimConfiguration();

		const env: any = {
			...process.env,
            devSkimProperties,
		};

		let debugOptions = {
			execArgv: ["--nolazy", "--inspect=6004"],
			env,
		};

		// If the extension is launched in debug mode then the debug server options are used
		// Otherwise the run options are used
		let serverOptions: ServerOptions = {
			run: {
				module: serverModule,
				options: { env },
				transport: TransportKind.pipe,
			},
			debug: {
				module: serverModule,
				options: debugOptions,
				transport: TransportKind.pipe,
			},
		};

		// Options to control the language client
		// Register the server for plain text documents.  I haven't found a "Always do this" option, hence the exhaustive
		//listing here.  If someone else knows how to say "do this for *" that would be the preference
		let clientOptions: LanguageClientOptions = {
			documentSelector: getDocumentSelectors(),
			synchronize: {
				// Synchronize the setting section 'devskim' to the server
				configurationSection: 'devskim',
				// Notify the server about file changes to '.clientrc files contain in the workspace
				fileEvents: vscode.workspace.createFileSystemWatcher('**/.clientrc'),
			},
		};

		client = new LanguageClient('Devskim', 'Devskim', serverOptions, clientOptions);

		// Create the language client and start the client.
		console.log(`extension: starting client ... `);
		let disposable = client.start();

		// Push the disposable to the context's subscriptions so that the 
		// client can be deactivated on extension deactivation
		context.subscriptions.push(disposable,
			vscode.commands.registerCommand('devskim.applySingleFix', applyTextEdits),
			vscode.commands.registerCommand('devskim.scanWorkspace', commandScanEverything),
			vscode.commands.registerCommand('devskim.reloadRules', commandReloadRules)
		);

		//when the extension is first loading a lot of stuff is happening asynchronously in VS code
		//as a result, often the first analysis doesn't happen until after the user types.  This will
		//start the analysis a couple seconds after VS Code loads, so if the user doesn't do anything 
		//an analysis still happens

		// @todo  This has a code smell and should be looked at - Dave
		setTimeout(function () {
			const textDocuments: TextDocumentIdentifier[] = [];
			for (let x = 0; x < vscode.workspace.textDocuments.length; x++) {
				textDocuments[x] = Object.create(null);
				textDocuments[x].uri = vscode.workspace.textDocuments[x].uri.toString();
			}
			client.sendRequest(ValidateDocsRequest.type, {textDocuments});
		}, 30000);

	} catch (err) {
		handleError(err);
	}

}

function getDevSkimConfiguration(section='devskim' ): DevSkimSettings {
	let settings: DevSkimSettings = new DevSkimSettingsObject();
	settings.enableBestPracticeRules = vscode.workspace.getConfiguration(section).get('enableBestPracticeRules', false);
	settings.enableDefenseInDepthSeverityRules = vscode.workspace.getConfiguration(section).get('enableDefenseInDepthSeverityRules', false);
	settings.enableInformationalSeverityRules = vscode.workspace.getConfiguration(section).get('enableInformationalSeverityRules', false);
	settings.enableLowSeverityRules = vscode.workspace.getConfiguration(section).get('enableLowSeverityRules', false);
	settings.enableManualReviewRules = vscode.workspace.getConfiguration(section).get('enableManualReviewRules', false);
	settings.guidanceBaseURL = vscode.workspace.getConfiguration(section).get('guidanceBaseURL', "https://github.com/Microsoft/DevSkim/blob/master/guidance/");
	settings.ignoreFiles = vscode.workspace.getConfiguration(section).get('ignoreFiles',
		[ "out/*", "bin/*", "node_modules/*", ".vscode/*", "yarn.lock", "logs/*", "*.log", "*.git" ]);
	settings.ignoreRulesList = vscode.workspace.getConfiguration(section).get('ignoreRulesList', []);
	settings.manualReviewerName = vscode.workspace.getConfiguration(section).get('manualReviewerName', '');
	settings.removeFindingsOnClose = vscode.workspace.getConfiguration(section).get('removeFindingsOnClose', false);
	settings.suppressionDurationInDays = vscode.workspace.getConfiguration(section).get('suppressionDurationInDays', 30);
	settings.validateRulesFiles = vscode.workspace.getConfiguration(section).get('validateRulesFiles', true);
	return settings;

}

function handleError(err: any) {
	const message = `Could not start DevSkim Server: [${err.message}]".`;
	vscode.window.showErrorMessage(message, { modal: false })
}

/**
 * Triggered when the user clicks a specific DevSkim code action (set in the server component in connection.OnCodeAction)
 * this function makes the actual code transformation corresponding to the action
 * 
 * @param {string} uri - the path to the document the edits should apply to
 * @param {number} documentVersion - the version of the file to apply the edits.  if the version doesn't match
 * 									 the current version the edit may no longer be applicable (this shouldn't happen)
 * @param {TextEdit[]} edits - the actual changes to make (range, text to replace, etc.)
 */
function applyTextEdits(uri: string, documentVersion: number, edits: TextEdit[]) {
		let textEditor = vscode.window.activeTextEditor;
		//make sure the code action triggered is against the current document (abundance of caution - the user shouldn't
		//be able to trigger an action for a different document).  Also make sure versions match.  This also shouldn't happen
		//as any changes to the document should refresh the code action, but since things are asynchronous this might be possible
		if (textEditor && textEditor.document.uri.toString() === uri) {
			if (textEditor.document.version !== documentVersion) {
				vscode.window.showInformationMessage(`DevSkim fixes are outdated and can't be applied to the document.`);
			}
			//apply the edits
			textEditor.edit(mutator => {
				for (let edit of edits) {
					mutator.replace(client.protocol2CodeConverter.asRange(edit.range), edit.newText);
				}
			}).then((success) => {
				if (!success) {
					vscode.window.showErrorMessage('Failed to apply DevSkim fixes to the document. Please consider opening an issue with steps to reproduce.');
				}
			});
		}
}

function commandReloadRules() {
	client.sendRequest(ReloadRulesRequest.type, null);
}

function commandScanEverything() {
	if (vscode.workspace.workspaceFolders) {
		let dir = require('node-dir');
		let [rootFolder] = vscode.workspace.workspaceFolders;
		if (rootFolder && rootFolder.uri && rootFolder.uri.fsPath) {
			dir.files(rootFolder.uri.fsPath, function (err: any, files: [any]) {
				if (err) throw err;

				for (let curFile of files) {
					if (curFile.indexOf(".git") == -1) {
						vscode.workspace.openTextDocument(curFile).then(doc => {
							const textDocuments: TextDocumentIdentifier[] = [];
							const td: TextDocumentIdentifier = Object.create(null);
							td.uri = doc.fileName;
							textDocuments.push(td);
							client.sendRequest(ValidateDocsRequest.type, {textDocuments});
						});
					}
				}
			});
		}
	}
}

