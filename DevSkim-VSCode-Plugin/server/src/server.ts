import * as net from 'net';
import DevSkimServer from './devskimServer'
import
{
    Connection,createConnection, InitializeParams, InitializeResult, ProposedFeatures, TextDocuments,
} from "vscode-languageserver";

import { StreamMessageReader, StreamMessageWriter } from "vscode-jsonrpc";
import ReadableStream = NodeJS.ReadableStream;
import WritableStream = NodeJS.WritableStream;

const pkg = require('../package');

export let connectionCtr = 0;

export class DevSkimMain
{
    static instance: DevSkimMain = undefined;
    connection: Connection = undefined;

    constructor()
    {
        if (DevSkimMain.instance != undefined)
        {
            return DevSkimMain.instance;
        }
        DevSkimMain.instance = this;
    }

    public listen(): void
    {
        if (this.connection === undefined)
        {
            connectionCtr++;
            let pipeName = '';
            let bToPipe = false;

            console.log(`index: listen(${connectionCtr})`);
            let idxOfPipe = process.argv.indexOf('--pipe');
            console.log(`index: listen(idxOfPipe - ${idxOfPipe}, process.argv.length - ${process.argv.length})`);

            if (idxOfPipe !== -1 && ((process.argv.length - 2) >= idxOfPipe))
            {
                pipeName = process.argv[idxOfPipe + 1];
                bToPipe = true;
            }

            this.connection = bToPipe
                ? this.createConnectionToPipes(pipeName)
                : createConnection(ProposedFeatures.all);

            const documents: TextDocuments = new TextDocuments();

            this.connection.onInitialize((params: InitializeParams): Promise<InitializeResult> =>
            {
                this.connection.console.log(`Initialized server v. ${pkg.version}`);
                return DevSkimServer.initialize(documents, this.connection, params)
                    .then(async server =>
                    {
                        await server.loadRules();
                        await server.register(this.connection);
                        return server;
                    })
                    .then((server) => ({
                        capabilities: server.capabilities(),
                    }));
            });

            documents.listen(this.connection);
            this.connection.console.log(`index: now listening on documents ...`);

            this.connection.listen();
            this.connection.console.log(`index: now listening on connection ...`);
        }
    }

    private createConnectionToPipes(pipeName)
    {

        const pipes = this.createPipes(pipeName);
        return createConnection(
            new StreamMessageReader(pipes[0] as ReadableStream),
            new StreamMessageWriter(pipes[1] as WritableStream),
        );
    }

    private createPipes(pipeName)
    {
        const pipePath = '\\\\.\\pipe\\';
        const iPipeName = 'devskiminput';
        const oPipeName = 'devskimoutput';

        const iPipe = net.createConnection(`${pipePath}${iPipeName}`, () =>
        {
            console.log(`Connected to input pipe`);
        });
        const oPipe = net.createConnection(`${pipePath}${oPipeName}`, () =>
        {
            console.log(`Connected to output pipe`);
        });
        return [iPipe, oPipe];
    }
}
