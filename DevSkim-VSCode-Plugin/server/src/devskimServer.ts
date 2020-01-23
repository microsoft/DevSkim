/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 *
 * Bulk of LSP logic
 * 
 * @export
 * @class DevSkimServer
 */
import
{
    CodeActionParams, Connection, Diagnostic, DidChangeConfigurationParams, InitializedParams, Hover,
    InitializeParams, RequestType, ServerCapabilities, TextDocument, TextDocuments, TextDocumentPositionParams    
} from "vscode-languageserver";

import { Command, TextEdit } from 'vscode-languageserver-protocol';
import { TextDocumentIdentifier } from 'vscode-languageserver-types';

import { AutoFix, DevSkimProblem, Fixes, IDevSkimSettings } from "./devskimObjects";
import {DevSkimWorker} from "./devskimWorker";
import { DevSkimWorkerSettings } from "./devskimWorkerSettings";
import { DevSkimSuppression } from "./utility_classes/suppressions";
import { DebugLogger } from "./utility_classes/logger"

/**
 * The specific implementation of the DevSkim LSP
 */
export default class DevSkimServer
{

    public static instance: DevSkimServer;

    /**
     * Sets up the DevSkim Server - not called directly but rather through initialize
     * @param documents files to analyze
     * @param connection the connection to the client
     * @param worker an instantiated instance of the DevSkimWorker class that does the analysis
     */
    private constructor(private documents: TextDocuments, private connection: Connection, private worker: DevSkimWorker)
    {
        this.globalSettings = worker.dswSettings.getSettings();
    }

    /**
     * Create an instance of the devskim server
     * @param documents files to analyze
     * @param connection connection to the client
     * @param params parameters the client passed during initialization
     */
    public static async initialize(documents: TextDocuments, connection: Connection, params: InitializedParams): Promise<DevSkimServer>
    {
        const dsWorkerSettings = new DevSkimWorkerSettings();
        const dsSettings = dsWorkerSettings.getSettings();
        const dsSuppression = new DevSkimSuppression(dsSettings);
        const logger : DebugLogger = new DebugLogger(dsSettings,connection);

        const worker = new DevSkimWorker(logger, dsSuppression, dsSettings);
        DevSkimServer.instance = new DevSkimServer(documents, connection, worker);
        return DevSkimServer.instance;
    }

    /**
     * load the rules from the file system
     */
    public async loadRules(): Promise<void>
    {
        return this.worker.init();
    }

    /**
     * registers the various event handlers the client can invoke
     * @param connection the connection to the client
     */
    public register(connection: Connection): void
    {
        this.documents.listen(this.connection);
        
        // connection handlers
        connection.onInitialize(this.onInitialize.bind(this));
        connection.onCodeAction(this.onCodeAction.bind(this));
        connection.onDidChangeConfiguration(this.onDidChangeConfiguration.bind(this));
        connection.onHover(this.onHover.bind(this));
        connection.onRequest(ReloadRulesRequest.type, this.onRequestReloadRulesRequest.bind(this));
        connection.onRequest(ValidateDocsRequest.type, this.onRequestValidateDocsRequest.bind(this));

        // document handlers
        this.documents.onDidOpen(this.onDidOpen.bind(this));
        this.documents.onDidClose(this.onDidClose.bind(this));
        this.documents.onDidChangeContent(this.onDidChangeContent.bind(this));
    }

    /**
     * informs the client of the capabilities this LSP exposes
     */
    public capabilities(): ServerCapabilities
    {
        // @todo: review this to find the best implementation
        return {
            // Tell the client that the server works in FULL text document sync mode
            textDocumentSync: this.documents.syncKind,
            codeActionProvider: true,
        };
    }

    /**
     * event handler for document opening event
     * @param change change of state from the IDE
     */
    private onDidOpen(change)
    {
        this.connection.console.log(`DevSkimServer: onDidOpen(${change.document.uri})`);
        // this.validateTextDocument(change.document);
    }

    /**
     * event handler for a document closing
     * @param change  change of state from the IDE
     */
    private onDidClose(change)
    {
        if (this.globalSettings.removeFindingsOnClose)
        {
            let diagnostics: Diagnostic[] = [];
            this.connection.sendDiagnostics({ uri: change.document.uri, diagnostics });
        }
    }

    /**
     * document handler for content changing
     * @param change  change of state from the IDE
     */
    private onDidChangeContent(change): Promise<void>
    {
        this.connection.console.log(`DevSkimServer: onDidChangeContent(${change.document.uri})`);
        if(change && change.document)
        {
            return this.validateTextDocument(change.document);
        }
        return;
    }

    /**
     * event handler for IDE initializing plugin
     * @param params connection initialization values
     */
    private onInitialize(params: InitializeParams): void
    {
        let capabilities = params.capabilities;
        this.hasConfigurationCapability = !!(capabilities.workspace && !!capabilities.workspace.configuration);
        this.hasWorkspaceFolderCapability = !!(capabilities.workspace && !!capabilities.workspace.workspaceFolders);

        this.hasDiagnosticRelatedInformationCapability = !!(
            capabilities.textDocument &&
            capabilities.textDocument.publishDiagnostics &&
            capabilities.textDocument.publishDiagnostics.relatedInformation
        );
        this.workspaceRoot = params.rootPath;
    }

    /**
     * Event handler for request from IDE to validate files
     * @param params collection of documents/settings to validate
     */
    private onRequestValidateDocsRequest(params: ValidateDocsParams): void
    {
        for (let docs of params.textDocuments)
        {
            let textDocument = this.documents.get(docs.uri);

            this.connection.console.log(`DevSkimServer: onRequestValidateDocsRequest(${textDocument.uri})`);
            this.validateTextDocument(textDocument);
        }
    }

    /**
     * Custom event handler for DevSkim specific event that reloads rules from the file system
     * typically called when files are being edited
     */
    private onRequestReloadRulesRequest()
    {
        this.worker.refreshAnalysisRules();
    }

    /**
     * Event Handler for requests for code actions
     * @param params context for the code actions being requested
     */
    private onCodeAction(params: CodeActionParams): Command[]
    {
        this.codeActions = [];
        let uri = params.textDocument.uri;
        let edits = this.worker.codeActions[uri];

        if (!edits)
        {
            return;
        }

        let fixes = new Fixes(edits);
        if (fixes.isEmpty())
        {
            return;
        }

        let documentVersion = -1;

        function createTextEdit(editInfo: AutoFix): TextEdit
        {
            return TextEdit.replace(editInfo.edit.range, editInfo.edit.text || '');
        }

        for (let editInfo of fixes.getScoped(params.context.diagnostics))
        {
            documentVersion = editInfo.documentVersion;
            this.codeActions.push(Command.create(editInfo.label, 'devskim.applySingleFix', uri, documentVersion,
                [createTextEdit(editInfo)]));
        }
        return this.codeActions;
    }

    /**
     * Event Handler for settings changing
     * @param change the new settings after the change
     */
    private onDidChangeConfiguration(change: DidChangeConfigurationParams): void
    {
        //this was part of the template but I basically ignore it.  The settings should
        //be updated to allow rulesets to be turned on and off, and this is where we would
        //get notified that the user did so
        if (this.hasConfigurationCapability)
        {
            this.documentSettings.clear();
        } 
        else
        {
            if(change.settings != undefined)
            {
                this.globalSettings = (change.settings.devskim) ? change.settings.devskim : change.settings;
            }          
        }

        // Revalidate any open text documents
        this.documents.all().forEach((td: TextDocument) =>
        {
            this.connection.console.log(`DevSkimServer: onDidChangeConfiguration(${td.uri})`);
            return this.validateTextDocument(td);
        });
    }

    /**
     * event handler for hover event
     * @param pos position of hover
     */
    private onHover(pos: TextDocumentPositionParams): Promise<Hover>
    {
        this.connection.console.log(`onHover: ${pos.position.line}:${pos.position.character}`);
        return null;
    }

    /**
     * retrieve the settings used for analyzing the document
     * @param resource the identifier for the object we need to retrieve settings for 
     */
    private getDocumentSettings(resource: string): Thenable<IDevSkimSettings>
    {
        if (!this.hasConfigurationCapability)
        {
            return Promise.resolve(this.globalSettings);
        }
        let result: any = this.documentSettings.get(resource);
        if (!result)
        {
            result = this.connection.workspace.getConfiguration({
                scopeUri: resource,
                section: 'devskim',
            });
            this.documentSettings.set(resource, result);
        }

        //if this is grabbed from the configuration than result isn't actually the settings object 
        //its an object with the settings object assigned to the "devskim" property
        result = (result.devskim != undefined) ? result.devskim : result;
        return result;
    }

    /**
     * Trigger an analysis of the provided document and record the code actions and diagnostics generated by the analysis
     *
     * @param {TextDocument} textDocument document to analyze
     */
    private async validateTextDocument(textDocument: TextDocument): Promise<void>
    {
        if (textDocument && textDocument.uri)
        {
            this.connection.console.log(`DevSkimServer: validateTextDocument(${textDocument.uri})`);
            let diagnostics: Diagnostic[] = [];
            let settings = await this.getDocumentSettings(textDocument.uri);
            if (!settings)
            {
                settings = this.globalSettings;
            }
            if (settings)
            {
                delete this.worker.codeActions[textDocument.uri];
                this.worker.UpdateSettings(settings);

                const problems: DevSkimProblem[] =
                    await this.worker.analyzeText(textDocument.getText(), textDocument.languageId, textDocument.uri);

                for (let problem of problems)
                {
                    let diagnostic: Diagnostic = problem.makeDiagnostic(this.worker.dswSettings);
                    diagnostics.push(diagnostic);

                    for (let fix of problem.fixes)
                    {
                        this.worker.recordCodeAction(textDocument.uri, textDocument.version,
                            diagnostic.range, diagnostic.code, fix, problem.ruleId);
                    }
                }
            }
            // Send the computed diagnostics to VSCode.
            this.connection.sendDiagnostics({ uri: textDocument.uri, diagnostics });
        }
    }

    private codeActions: Command[] = [];
    private diagnostics: Diagnostic[] = [];
    private documentSettings: Map<string, Thenable<IDevSkimSettings>> = new Map();
    private globalSettings: IDevSkimSettings;
    private hasConfigurationCapability = false;
    private hasWorkspaceFolderCapability = false;
    private hasDiagnosticRelatedInformationCapability = false;
    private workspaceRoot: string;
}

export class ReloadRulesRequest
{
    public static type = new RequestType<{}, void, void, void>('devskim/validaterules')
}

interface ValidateDocsParams
{
    textDocuments: TextDocumentIdentifier[];
}

export class ValidateDocsRequest
{
    public static type: RequestType<ValidateDocsParams, void, void, void> = new RequestType<ValidateDocsParams, void, void, void>(
        'textDocument/devskim/validatedocuments')
}


