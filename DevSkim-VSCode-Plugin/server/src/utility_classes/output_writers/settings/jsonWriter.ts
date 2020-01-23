/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * JSON output writer class for settings json
 * 
 */
import {DevskimSettingsWriter} from "../outputWriter";
import * as DevSkimObjects from "../../../devskimObjects";

export class JSONSettingsWriter extends DevskimSettingsWriter
{
    /**
     * Generate the output string that will either be written to the console or to a file by writeOutput
     * the base implementation for writeOutput calls the function and then writes out to the given location
     * so all that is necessary is defining the output to be written
     */      
    protected createOutput(): string
    {
        return JSON.stringify(this.devskimSettings , null, 4);
    }

    /**
     * Set up the interface
     * @param settings the settings being written to output
     */    
    initialize(settings: DevSkimObjects.IDevSkimSettings): void
    {
        super.initialize(settings);
        this.fileExtension = "json";
    }
    
}