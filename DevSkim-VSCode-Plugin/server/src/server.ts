/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
import {
	createConnection,
	TextDocuments,
	Diagnostic,
	DiagnosticSeverity,
	ProposedFeatures,
	InitializeParams,
	DidChangeConfigurationNotification,
	CompletionItem,
	CompletionItemKind,
	TextDocumentPositionParams,
	TextDocumentSyncKind,
	InitializeResult,
} from 'vscode-languageserver/node';
import * as cp from 'child_process';
import {
	TextDocument
} from 'vscode-languageserver-textdocument';
import { Stream } from 'stream';
import { CodeFixMapping } from './codeFixMapping';
import { getCodeFixMapping, getDevSkimPath, getDotNetPath, getSetSettings } from './notificationNames';
import { DevSkimSettings, DevSkimSettingsObject } from './devskimSettings';

// Create a connection for the server, using Node's IPC as a transport.
// Also include all preview / proposed LSP features.
const connection = createConnection(ProposedFeatures.all);

// Create a simple text document manager.
const documents: TextDocuments<TextDocument> = new TextDocuments(TextDocument);

let hasConfigurationCapability = false;
let hasWorkspaceFolderCapability = false;
let hasDiagnosticRelatedInformationCapability = false;

connection.onInitialize((params: InitializeParams) => {
	const capabilities = params.capabilities;

	// Does the client support the `workspace/configuration` request?
	// If not, we fall back using global settings.
	hasConfigurationCapability = !!(
		capabilities.workspace && !!capabilities.workspace.configuration
	);
	hasWorkspaceFolderCapability = !!(
		capabilities.workspace && !!capabilities.workspace.workspaceFolders
	);
	hasDiagnosticRelatedInformationCapability = !!(
		capabilities.textDocument &&
		capabilities.textDocument.publishDiagnostics &&
		capabilities.textDocument.publishDiagnostics.relatedInformation
	);

	const result: InitializeResult = {
		capabilities: {
			textDocumentSync: TextDocumentSyncKind.Incremental,
			
		}
	};
	if (hasWorkspaceFolderCapability) {
		result.capabilities.workspace = {
			workspaceFolders: {
				supported: true
			}
		};
	}
	return result;
});

connection.onInitialized(() => {
	if (hasConfigurationCapability) {
		// Register for all configuration changes.
		connection.client.register(DidChangeConfigurationNotification.type, undefined);
	}
	if (hasWorkspaceFolderCapability) {
		connection.workspace.onDidChangeWorkspaceFolders(_event => {
			connection.console.log('Workspace folder change event received.');
		});
	}
});

const defaultSettings: DevSkimSettings = new DevSkimSettingsObject();
let globalSettings: DevSkimSettings = defaultSettings;

let dotnetPath = '';
let devskimPath = '';

connection.onNotification(getDotNetPath(), (path: string) => 
{
	dotnetPath = path;
});

connection.onNotification(getDevSkimPath(), (path: string) => 
{
	devskimPath = path;
});

connection.onNotification(getSetSettings(), (settings: DevSkimSettings) => 
{
	globalSettings = settings;
});

connection.onDidChangeConfiguration(change => {
	globalSettings = <DevSkimSettings>(
		(change.settings.devskim || defaultSettings)
	);
	// Revalidate all open text documents
	documents.all().forEach(validateTextDocument);
});

// Only keep settings for open documents
documents.onDidClose(e => {
	if (globalSettings.removeFindingsOnClose)
	{
		const diagnostics: Diagnostic[] = [];
		connection.sendDiagnostics({ uri: e.document.uri, diagnostics });
	}
});

// The content of a text document has changed. This event is emitted
// when the text document first opened or when its content has changed.
documents.onDidChangeContent(change => {
	validateTextDocument(change.document);
});

async function validateTextDocument(textDocument: TextDocument): Promise<void> {
	if (dotnetPath == '')
	{
		connection.console.log("dotnetPath is not configured and server cannot execute.");
		return;
	}
	if (devskimPath == '')
	{
		connection.console.log("devskimPath is not configured and server cannot execute.");
		return;
	}
	const settings = globalSettings;
	const fileNameOnly = textDocument.uri.split('/').slice(-1)[0];
	const text = textDocument.getText();
	const stdInStream = new Stream.Readable();
	stdInStream.push(text);
	// sends EOF so devskim knows the input has ended.
	stdInStream.push(null);
	try
	{
		const severity = ['critical','important','moderate'];
		if (settings.enableBestPracticeRules)
		{
			severity.push('practice');
		}
		if (settings.enableManualReviewRules)
		{
			severity.push('manual');
		}
		
		const args = [devskimPath, 'analyze', fileNameOnly, '-f', 'json', '--simple-replacements', '-o', '%F%L%C%l%c%R%N%S%D%V', '--useStdIn', '-s', severity.join(','), '--ignore-regex', `"${settings.ignoreFiles.join(',')}"`];
		if (settings.ignoreRulesList.length > 0){
			args.push('--ignore-rule-ids');
			args.push(settings.ignoreRulesList);
		}
		const child = cp.spawn(dotnetPath, args);
		
		let theOutput = '';
		let theError = '';
		child.stdout.on('data', data => {
			theOutput += data;
		});
		child.stderr.on('data', data => {
			theError += data;
		});
		child.on('error', err => {
			connection.console.log(`Failed to spawn DevSkim. ${err.message}`);
		});
		child.on('exit', function (code, signal) {
			if (code == 0)
			{
				const diagnostics = parseToDiagnostics(theOutput, textDocument.uri.toString());
				connection.sendDiagnostics({ uri: textDocument.uri, diagnostics });
			}
			else
			{
				connection.console.log('DevSkim process exited with ' +
					`code ${code} and signal ${signal}`);
			}
		});
		stdInStream.pipe(child.stdin);
	}
	catch(err){
		connection.console.log(`${err}`);
	}
}

function parseToDiagnostics(jsonOutputFromDevskim: string, uri: string) : Diagnostic[]
{
	const deserialized = JSON.parse(jsonOutputFromDevskim);
	const diags : Diagnostic[] = [];
	for(const finding in deserialized)
	{
		const diagnostic: Diagnostic = {
			severity: DevSkimSeverityToDiagnosticSeverity(deserialized[finding]["severity"]),
			range: {
				start: { line: parseInt(deserialized[finding]["start_line"])-1, character: parseInt(deserialized[finding]["start_column"]) - 1},
				end: { line: parseInt(deserialized[finding]["end_line"])-1, character: parseInt(deserialized[finding]["end_column"]) - 1}
			},
			message: deserialized[finding]["description"],
			source: `[${deserialized[finding]["rule_id"]}] ${deserialized[finding]["rule_name"]}`,
			code: "MS-CST-E.vscode-devskim"
		};
		diags.push(diagnostic);
		if (deserialized[finding]["processed_fixes"] != undefined)
		{
			for(const fix in deserialized[finding]["processed_fixes"])
			{
				connection.sendNotification(getCodeFixMapping(), new CodeFixMapping(diagnostic, deserialized[finding]["processed_fixes"][fix], uri));
			}
		}
	}
	return diags;
}

connection.onDidChangeWatchedFiles(_change => {
	// Monitored files have change in VSCode
	connection.console.log('We received an file change event');
});

// This handler provides the initial list of the completion items.
connection.onCompletion(
	(_textDocumentPosition: TextDocumentPositionParams): CompletionItem[] => {
		// The pass parameter contains the position of the text document in
		// which code complete got requested. For the example we ignore this
		// info and always provide the same completion items.
		return [
			{
				label: 'TypeScript',
				kind: CompletionItemKind.Text,
				data: 1
			},
			{
				label: 'JavaScript',
				kind: CompletionItemKind.Text,
				data: 2
			}
		];
	}
);

// This handler resolves additional information for the item selected in
// the completion list.
connection.onCompletionResolve(
	(item: CompletionItem): CompletionItem => {
		if (item.data === 1) {
			item.detail = 'TypeScript details';
			item.documentation = 'TypeScript documentation';
		} else if (item.data === 2) {
			item.detail = 'JavaScript details';
			item.documentation = 'JavaScript documentation';
		}
		return item;
	}
);

// Make the text document manager listen on the connection
// for open, change and close text document events
documents.listen(connection);

// Listen on the connection
connection.listen();
function DevSkimSeverityToDiagnosticSeverity(arg0: string): DiagnosticSeverity | undefined {
	switch(arg0)
	{
		case "None":
			return DiagnosticSeverity.Information;
		case "Critical":
			return DiagnosticSeverity.Error;
		case "Important":
			return DiagnosticSeverity.Warning;
		case "Moderate":
			return DiagnosticSeverity.Warning;
		case "BestPractice":
			return DiagnosticSeverity.Hint;
		case "ManualReview":
			return DiagnosticSeverity.Hint;
		default:
			return DiagnosticSeverity.Information;
	}
}

