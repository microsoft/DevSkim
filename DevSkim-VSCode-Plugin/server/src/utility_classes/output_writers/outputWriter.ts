/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * Handles writing the file output in SARIF v2.1 RTM 4 format
 * 
 */
import * as DevSkimObjects from "../../devskimObjects";


/**
 * Abstract base class that all of the file writers (settings, results, etc.) share, to cut down
 * on duplicate code, and make polymorphism easier when generally referencing one of a number of
 * "writer" classes
 */
export abstract class DevSkimFileWriter
{
    //settings object that this run of DevSkim analysis executed with
    protected devskimSettings : DevSkimObjects.IDevSkimSettings;
    protected outputLocation : string = "";
    
    protected fileExtension: string = "txt";
    protected defaultFileName: string = "devskim_results";

  /**
     * Get the default file name that output will be written to, absent a user specified file name
     * @return the default file name. to be used if no file name was provided from the command line
     */        
    public getDefaultFileName(): string
    {
        return this.defaultFileName + "." + this.fileExtension;
    }
    

    /**
     * Sets where the output is sent.  If an empty string, output is echoed to the console, otherwise the output is 
     * used as a file name.  If not a full path, it will write to the current working directory
     * @param outputLocale location to write output to
     */    
    public setOutputLocale(outputLocale: string): void
    {
        //add a file extension if they left it off
        if(outputLocale.length > 0 && outputLocale.indexOf(".") == -1)
        {
            outputLocale = outputLocale + "." + this.fileExtension;
        }
        this.outputLocation = outputLocale;
    }

    /**
     * Output the current findings that have been added with createRun.  This will use the file path
     * specified during the setOutputLocale call, and will overwrite any existing file already there. Will write in the appropriate format for each class
     * determined by the call to createOutput
     * 
     * If the outputLocation string is an empty string, it will instead be written to the console
     */     
    public writeOutput(): void
    {
        let output : string = this.createOutput();

        if(this.outputLocation.length == 0)
        {
            console.log(output);
        }
        else
        {
            let fs  = require("fs");
        
            fs.writeFile(this.outputLocation, output, (err)=> {});  
            console.log(this.createOutputConfirmationMessage());
            console.log();
        }
    }
    
    /**
     * Generate the output string that will either be written to the console or to a file by writeOutput
     * the base implementation for writeOutput calls the function and then writes out to the given location
     * so all that is necessary is defining the output to be written
     */  
    protected abstract createOutput() : string; 
    
    /**
     * Create a short message telling the user the nature of the output created and where it was written
     * e.g. "A template of the DevSkim settings configuration was written to devskim_settings.json"
     */
    protected abstract createOutputConfirmationMessage() : string;
}

/**
 * Base class that all of the result writers share, adding some functions on top of the base DevSkimFileWriter class
 * and creating implementations common to all of the result writers
 */
export abstract class DevSkimResultsWriter extends DevSkimFileWriter
{
    protected workingDirectory : string;
    protected defaultFileName: string = "devskim_results";

     /**
     * Set up initial values
     * @param settings the settings that this instance of DevSkim Analysis was with
     * @param analyzedDirectory directory that was analyzed (NOT the directory to the output is written to - that will go in the same directory devskim was run from)
     */    
    public initialize(settings: DevSkimObjects.IDevSkimSettings, analyzedDirectory: string): void
    {
        this.devskimSettings = settings;
        this.workingDirectory = analyzedDirectory;
    }

    /**
     * Create a short message telling the user the nature of the output created and where it was written
     */    
    protected createOutputConfirmationMessage() : string
    {
        return "Analyzed all files under \""+this.workingDirectory+"\" and wrote the findings to " + this.outputLocation; 
    }    

    /**
     * Each folder with git repo info and files should go under its own run, as well as the parent directory
     * if it contains files, even if it does not have git info.  This populates information to be written out
     * from that run
     * @param analysisRun all of the information from the analysis of a directory and its contents/sub-directories 
     */    
    public abstract createRun(analysisRun: DevSkimObjects.Run): void;
}

export abstract class DevskimSettingsWriter extends DevSkimFileWriter
{
    protected defaultFileName: string = "devskim_settings";

    /**
     * Set up the interface
     * @param settings the settings being written to output
     */    
    public initialize(settings: DevSkimObjects.IDevSkimSettings): void
    {
        this.devskimSettings = settings;
        
        //remove settings irrelevant for the CLI
        delete this.devskimSettings.suppressionDurationInDays;
        delete this.devskimSettings.manualReviewerName;
        delete this.devskimSettings.suppressionCommentStyle;
        delete this.devskimSettings.suppressionCommentPlacement;
        delete this.devskimSettings.removeFindingsOnClose;
        delete this.devskimSettings.toolInfo;        
    }
    
    /**
     * Create a short message telling the user the nature of the output created and where it was written
     */    
    protected createOutputConfirmationMessage() : string
    {
        return "Creating a settings template file to be customize analysis runs, and wrote the file to " + this.outputLocation; 
    }        
}

/** various formats that the output may be written as */
export enum OutputFormats
{
    Text,
    SARIF21,
    HTML,
    CSV,
    JSON
}


