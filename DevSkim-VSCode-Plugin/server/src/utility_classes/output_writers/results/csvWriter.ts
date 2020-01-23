/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * CSV results output writer class
 * 
 */

import * as DevSkimObjects from "../../../devskimObjects";
import { DevSkimResultsWriter} from "../outputWriter";

/**
 * Class to write output as comma separated valued
 * The correct order to use this is initialize, (optional) setOutputLocale, createRun for each run, writeOutput 
 */
export class CSVResultWriter extends DevSkimResultsWriter
{

     /**
     * Set up the HTML object, recording the settings this analysis was run under, and
     * the top level tool information (version, schema, etc.)
     * @param settings the settings that this instance of DevSkim Analysis was with
     * @param analyzedDirectory directory that was analyzed (NOT the directory to the output is written to - that will go in the same directory devskim was run from)
     */
    initialize(settings: DevSkimObjects.IDevSkimSettings, analyzedDirectory: string): void
    {
        super.initialize(settings,analyzedDirectory);
        this.fileExtension = "csv";
    }    
    
    /**
     * Each folder with git repo info and files should go under its own run, as well as the parent directory
     * if it contains files, even if it does not have git info.  This populates information to be written out
     * from that run
     * @param analysisRun all of the information from the analysis of a directory and its contents/sub-directories 
     */
    createRun(analysisRun: DevSkimObjects.Run): void
    {
        throw new Error('Method not implemented.');
    }

    /**
     * Generate the output string that will either be written to the console or to a file by writeOutput
     * the base implementation for writeOutput calls the function and then writes out to the given location
     * so all that is necessary is defining the output to be written
     */      
    protected createOutput(): string
    {
        throw new Error('Method not implemented.');
    }    
    
}