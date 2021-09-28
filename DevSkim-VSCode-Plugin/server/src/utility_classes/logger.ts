// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/* ------------------------------------------------------------------------------------------ 
 * debug class to help with debugging/logging.  Controlled by settings
 *
 */


import { Connection } from "vscode-languageserver";
import 
{
     IDevSkimSettings,
}    from "../devskimObjects";

/**
 * 
 */
export class DebugLogger
{
    private debugConsole;

    constructor(private settings: IDevSkimSettings, private connection?: Connection)
    {
        this.debugConsole = (connection) ? connection.console : console;
    }
    
    public log(...args: any[])
    {
        if(this.settings.debugLogging)
        {
            this.debugConsole.log.apply(console,args);
        }
    }
}