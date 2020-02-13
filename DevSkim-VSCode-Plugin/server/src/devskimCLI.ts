/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * command line class for the CLI version of DevSkim - invoked from cli.ts.  
 * 
 */


import { IDevSkimSettings, DevSkimProblem, Rule, FileInfo, DirectoryInfo, Run } from "./devskimObjects";

import { DevSkimWorker } from "./devskimWorker";
import { PathOperations } from "./utility_classes/pathOperations";
import { DevSkimWorkerSettings } from "./devskimWorkerSettings";
import { DevSkimSuppression } from "./utility_classes/suppressions";
import { DebugLogger } from "./utility_classes/logger";
import { DevskimSettingsWriter, OutputFormats, DevSkimResultsWriter } from "./utility_classes/output_writers/outputWriter";
import { gitHelper } from './utility_classes/git';
import { TextResultWriter } from './utility_classes/output_writers/results/textWriter';
import { SARIF21ResultWriter } from './utility_classes/output_writers/results/sarif21Writer';
import { HTMLResultWriter } from './utility_classes/output_writers/results/htmlWriter';
import { CSVResultWriter } from './utility_classes/output_writers/results/csvWriter';
import { JSONSettingsWriter } from './utility_classes/output_writers/settings/jsonWriter';

/**
 * An enum to track the various potential commands from the CLI
 */
export enum CLIcommands
{
    /**
     * Run an analysis of the files under a folder
     */
    Analyze = "analyze",

    /**
     * List the rules, optionally producing validation
     */
    inventoryRules = "rules",

    /**
     * List the rules, optionally producing validation
     */
    showSettings = "settings",
}

/**
 * The main worker class for the Command Line functionality
 */
export class DevSkimCLI
{
    private workingDirectory : string;
    private settings : IDevSkimSettings;
    private outputFilePath: string;
    private outputObject : DevSkimResultsWriter | DevskimSettingsWriter;

    /**
     * Set up the CLI class - does not run the command, just sets everything up 
     * @param command the command run from the CLI
     * @param options the options used with that command
     */
    constructor(private command : CLIcommands, private options)
    {
        this.workingDirectory = (this.options === undefined || this.options.directory === undefined  || this.options.directory.length == 0 ) ? 
            process.cwd() :  this.options.directory;
        
        this.buildSettings();  
        this.setOutputObject();        
    }

    /**
     * Create a DevSkimSettings object from the specified command line options (or defaults if no relevant option is present)
     */
    private buildSettings() 
    {
        //right now most of the options come from the defaults, with only a couple of variations passed
        //from the command line
        this.settings = DevSkimWorkerSettings.defaultSettings();
    
        if(this.options.best_practice != undefined && this.options.best_practice == true)
        {
            this.settings.enableBestPracticeRules = true;
        }
    
        if(this.options.manual_review != undefined && this.options.manual_review == true)
        {
            this.settings.enableManualReviewRules = true;
        }    
    }

    /**
     * Sets up the output object
     * this is going to be an ugly function with a lot of conditionals and very little actual code
     * since the point is to figure out all of the format/file output permutations and set properties
     * and objects accordingly
     */
    private setOutputObject()
    {
        let format : OutputFormats;
        let fileExt : string = "t";

        //First, lets figure out what sort of format everything is supposed to be
        //if they didn't specify a format with the -f/--format switch, see if they
        //specified a file to output to.  If so, hint from the file extension.  If it
        //doesn't have one, then lets just assume text, because ¯\_(ツ)_/¯
        if(this.options === undefined || this.options.format === undefined)
        {
            if(this.options !== undefined && this.options.output_file !== undefined )
            {
                fileExt = this.options.output_file.toLowerCase();
                if(fileExt.indexOf(".") > -1)
                {
                    fileExt = fileExt.substring(fileExt.lastIndexOf(".")+1);       
                }
            }   
                 
        }
        else //hey, they specified a format, lets go with that
        {
            fileExt = this.options.format.toLowerCase();
        }

        //the format specified with -f, or the file extension if missing
        //could be in all sorts of forms.  Instead of being pedantic and 
        //hard to use, lets see if it matches against a loose set of possibilities
        switch(fileExt)
        {
            case "s":
            case "sarif":
            case "sarif2.1":
            case "sarif21": format = OutputFormats.SARIF21;
                break;
            
            case "h":
            case "htm":
            case "html": format = OutputFormats.HTML;
                break;
            
            case "c":
            case "csv": format = OutputFormats.CSV;
                break;

            case "j":
            case "jsn":
            case "json": format = OutputFormats.JSON;
                break;
            default: format = OutputFormats.Text;
        }
    

        //now we know what format, lets create the correct object
        //Unfortunately the correct output object is the union of what command we
        //are executing and the specified format, so ugly nested switch statements
        switch(this.command)
        {
            case CLIcommands.Analyze:
            {
                switch(format)
                {
                    case OutputFormats.SARIF21: this.outputObject = new SARIF21ResultWriter();
                        break;                
                    case OutputFormats.HTML: this.outputObject = new HTMLResultWriter();
                        break;                
                    case OutputFormats.CSV: this.outputObject = new CSVResultWriter();
                        break;                
                    default: this.outputObject = new TextResultWriter;
                }
                this.outputObject.initialize(this.settings, this.workingDirectory );
                break;
            }   

            case CLIcommands.showSettings:
            {
                switch(format)
                {
                    case OutputFormats.JSON: this.outputObject = new JSONSettingsWriter();
                        break;
                    default: this.outputObject = new JSONSettingsWriter();
                }
                this.outputObject.initialize(this.settings);
                break;
            }
            
            default: throw new Error('Method not implemented.');
        }

        //now we need to determine where the actual output goes.  If -o isn't used
        //its going to the console.  Otherwise, it will either use a default file
        //name if they used -o but didn't pass a file name argument, or it will
        //go to the file name they specified
        if(this.options === undefined || this.options.output_file === undefined )
        {
            this.outputFilePath = "";
        }
        else if(this.options.output_file === true)
        {
            this.outputFilePath = this.outputObject.getDefaultFileName(); 
        }
        else
        {
            this.outputFilePath =  this.options.output_file;   
        }  

        this.outputObject.setOutputLocale(this.outputFilePath);  
    }

    /**
     * Run the command that was passed from the CLI
     */
    public async run()
    {
        switch(this.command)
        {
            case CLIcommands.Analyze: 
                let git : gitHelper = new gitHelper();
                await git.getRecursiveGitInfo(this.workingDirectory, 
                    directories => 
                    {
                        this.analyze(directories)
                    });
                break;
            case CLIcommands.inventoryRules: await this.inventoryRules();
                break;
            case CLIcommands.showSettings: this.outputObject.writeOutput();
                break;
        }
    }

    /**
     * Produce a template of the DevSkim settings to make it easier to customize runs
     * @todo do more than just output it to the command line, and finish fleshing out
     * the settings object
     */
    private writeSettings()
    {
        let settings : IDevSkimSettings = DevSkimWorkerSettings.defaultSettings();


        //remove settings irrelevant for the CLI
        delete settings.suppressionDurationInDays;
        delete settings.manualReviewerName;
        delete settings.suppressionCommentStyle;
        delete settings.suppressionCommentPlacement;
        delete settings.removeFindingsOnClose;

        let output : string = JSON.stringify(settings , null, 4);

        console.log(output);
    }
    
    /**
     * function invoked from command line. Right now a simplistic stub that simply lists the rules
     * @todo create HTML output with much better formatting/info, and optional validation
     */
    private async inventoryRules() : Promise<void>
    {
        const dsSuppression = new DevSkimSuppression(this.settings);
        const logger : DebugLogger = new DebugLogger(this.settings);
    
        var analysisEngine : DevSkimWorker = new DevSkimWorker(logger, dsSuppression, this.settings);
        await analysisEngine.init();
        let rules : Rule[] = analysisEngine.retrieveLoadedRules();
        for(let rule of rules)
        {
            console.log(rule.id+" , "+rule.name);
        }          
    }
    
    /**
     * Analyze the contents of provided directory paths, and output
     * @param directories collection of Directories that will be analyzed
     */
    private async analyze(directories : DirectoryInfo[] ) : Promise<void>
    {    
        let FilesToLog : FileInfo[] = [];   
    
        let dir = require('node-dir'); 
        dir.files(directories[0].directoryPath, async (err, files) => {        
            if (err)
            {
                console.log(err);
                 throw err;
            }
    
            if(files == undefined || files.length < 1)
            {
                console.log("No files found in directory %s", directories[0].directoryPath);
                return;
            }
            
            let fs = require("fs");            
            
            const dsSuppression = new DevSkimSuppression(this.settings);
            const logger : DebugLogger = new DebugLogger(this.settings);
    
            var analysisEngine : DevSkimWorker = new DevSkimWorker(logger, dsSuppression, this.settings);
            await analysisEngine.init();
    
            let pathOp : PathOperations = new PathOperations();
            var problems : DevSkimProblem[] = [];
            for(let directory of directories)
            {               
                for(let curFile of files)
                {						
                    if(!PathOperations.ignoreFile(curFile,this.settings.ignoreFiles))
                    {
                        //first check if this file is part of this run, by checking if it is under the longest path
                        //within the directory collection
                        let longestDir : string = "";
                        for(let searchDirectory of directories)
                        {
                            searchDirectory.directoryPath = pathOp.normalizeDirectoryPaths(searchDirectory.directoryPath);
                            if(curFile.indexOf(searchDirectory.directoryPath) != -1)
                            {
                                if (searchDirectory.directoryPath.length > longestDir.length)
                                {
                                    longestDir = searchDirectory.directoryPath;
                                }
                            }
                        }
                        //now make sure that whatever directory the file was associated with is the current directory being analyzed
                        if(pathOp.normalizeDirectoryPaths(longestDir) == pathOp.normalizeDirectoryPaths(directory.directoryPath))
                        {
                            //give some indication of progress as files are analyzed
                            console.log("Analyzing \""+curFile.substr(directories[0].directoryPath.length) + "\"");                    
    
                            let documentContents : string = fs.readFileSync(curFile, "utf8");
                            let langID : string = pathOp.getLangFromPath(curFile);
    
                            problems = problems.concat(analysisEngine.analyzeText(documentContents,langID, curFile, false));
    
                            //if writing to a file, add the metadata for the file that is analyzed
                            if(this.outputFilePath.length > 0)
                            {                      
                                FilesToLog.push(this.createFileData(curFile,documentContents,directory.directoryPath));                
                            }  
                        }          
                    }
                                            
                }
                if(problems.length > 0 || FilesToLog.length > 0)
                {
                    (<DevSkimResultsWriter>this.outputObject).createRun(new Run(directory, 
                                                            analysisEngine.retrieveLoadedRules(), 
                                                            FilesToLog, 
                                                            problems));                                
                    problems  = [];
                    FilesToLog = [];
                }
                
            }
            //just add a space at the end to make the final text more readable
            console.log("\n-----------------------\n");
            
            this.outputObject.writeOutput();

            
        });	
    }
    
    /**
     * Creates an object of metadata around the file, to identify it on disk both by path and by hash
     * @param curFile the path of the current file being analyzed
     * @param documentContents the contents of the document, for hashing
     * @param analysisDirectory the parent directory that analysis started at, used to create relative pathing
     */
    private createFileData(curFile : string, documentContents : string, analysisDirectory : string ) : FileInfo
    {
        const crypto = require('crypto');
        let pathOp : PathOperations = new PathOperations();
        let fs = require("fs"); 
    
        let fileMetadata : FileInfo = Object.create(null);
        //the URI needs to be relative to the directory being analyzed, so get the current file URI
        //and then chop off the bits for the parent directory
        fileMetadata.fileURI = pathOp.fileToURI(curFile);
        fileMetadata.fileURI = fileMetadata.fileURI.substr(pathOp.fileToURI(analysisDirectory).length+1);
        
        fileMetadata.sourceLanguageSARIF = pathOp.getLangFromPath(curFile, true);
        fileMetadata.sha256hash = crypto.createHash('sha256').update(documentContents).digest('hex');
        fileMetadata.sha512hash = crypto.createHash('sha512').update(documentContents).digest('hex');
        fileMetadata.fileSize = fs.statSync(curFile).size;
    
        return fileMetadata;
    }
}


