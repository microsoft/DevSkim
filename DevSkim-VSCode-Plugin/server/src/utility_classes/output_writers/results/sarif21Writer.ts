/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * SARIF v2.1 results output writer class
 * 
 */
import * as SARIF21Schema from "@schemastore/sarif-2.1.0-rtm.4";
import * as DevSkimObjects from "../../../devskimObjects";
import {PathOperations} from "../../pathOperations";
import {DevSkimResultsWriter} from "../outputWriter";

/**
 * Class to write output in SARIF v2.1 format
 * The correct order to use this is initialize, (optional) setOutputLocale, createRun for each run, writeOutput 
 */
export class SARIF21ResultWriter  extends DevSkimResultsWriter
{

    //settings object that this run of DevSkim analysis executed with
    private SarifFileObject : SARIF21Schema.StaticAnalysisResultsFormatSARIFVersion210Rtm4JSONSchema;


     /**
     * Set up the SARIF object, recording the settings this analysis was run under, and
     * the top level SARIF information (version, schema, etc.)
     * @param settings the settings that this instance of DevSkim Analysis was with
     * @param analyzedDirectory directory that was analyzed (NOT the directory to the output is written to - that will go in the same directory devskim was run from)
     */
    public initialize(settings: DevSkimObjects.IDevSkimSettings, analyzedDirectory: string)
    {
        
        super.initialize(settings,analyzedDirectory);
        this.fileExtension = "sarif";

        this.SarifFileObject = Object.create(null);
        this.SarifFileObject.version = "2.1.0";
        this.SarifFileObject.$schema =  "https://raw.githubusercontent.com/oasis-tcs/sarifspec/master/Schemata/sarif-schema-2.1.0.json";
        this.SarifFileObject.runs = []; 
    }
    
     /**
     * Get the default file name that output will be written to, absent a user specified file name
     * @return the default file name. to be used if no file name was provided from the command line
     */    
    public getDefaultFileName() : string
    {
        return "devskim_results.sarif";
    }

    /**
     * Sets where the output is sent.  If an empty string, output is echoed to the console, otherwise the output is 
     * used as a file name.  If not a full path, it will write to the current working directory
     * @param outputLocale location to write output to
     */    
    setOutputLocale(outputLocale : string) : void
    {
        //add a file extension if they left it off
        if(outputLocale.length > 0 && outputLocale.indexOf(".") == -1)
        {
            outputLocale = outputLocale + ".sarif";
        }
        this.outputLocation = outputLocale;
    }

    /**
     * Each folder with git repo info and files should go under its own run, as well as the parent directory
     * if it contains files, even if it does not have git info.  This populates information to be written out
     * from that run, adding them to the appropriate SARIF objects.  It also sets up the tool info for each run
     * @param analysisRun all of the information from the analysis of a directory and its contents/sub-directories 
     */
    public createRun(analysisRun : DevSkimObjects.Run) : void
    {
        let runNumber : number = this.SarifFileObject.runs.length;

        //common initializations independent of the run information
        this.SarifFileObject.runs[runNumber] = Object.create(null);
        this.SarifFileObject.runs[runNumber].tool = Object.create(null);
        this.SarifFileObject.runs[runNumber].tool.driver = Object(null);                   
        this.SarifFileObject.runs[runNumber].tool.driver.name = this.devskimSettings.toolInfo.name;
        this.SarifFileObject.runs[runNumber].tool.driver.fullName = this.devskimSettings.toolInfo.fullName;
        this.SarifFileObject.runs[runNumber].tool.driver.shortDescription = this.devskimSettings.toolInfo.shortDescription;
        this.SarifFileObject.runs[runNumber].tool.driver.fullDescription = this.devskimSettings.toolInfo.fullDescription;
        this.SarifFileObject.runs[runNumber].tool.driver.version = this.devskimSettings.toolInfo.version;
        this.SarifFileObject.runs[runNumber].tool.driver.semanticVersion = this.devskimSettings.toolInfo.semanticVersion;
        this.SarifFileObject.runs[runNumber].tool.driver.dottedQuadFileVersion = this.devskimSettings.toolInfo.dottedQuadFileVersion;
        this.SarifFileObject.runs[runNumber].tool.driver.organization = this.devskimSettings.toolInfo.organization;
        
        //we aren't guaranteed to have git info, but if its there, add it to the SARIF
        if(analysisRun.directoryInfo.gitRepo.length > 0)
        {
            this.SarifFileObject.runs[runNumber].versionControlProvenance = [];
            this.SarifFileObject.runs[runNumber].versionControlProvenance[0] = Object.create(null);
            this.SarifFileObject.runs[runNumber].versionControlProvenance[0].repositoryUri = analysisRun.directoryInfo.gitRepo;
            this.SarifFileObject.runs[runNumber].versionControlProvenance[0].branch = analysisRun.directoryInfo.gitInfo.branch;
            this.SarifFileObject.runs[runNumber].versionControlProvenance[0].revisionId = analysisRun.directoryInfo.gitInfo.sha;
        }

        this.SarifFileObject.runs[runNumber].originalUriBaseIds = Object.create(null);
        this.SarifFileObject.runs[runNumber].originalUriBaseIds = {"%SRCROOT%" : {"uri" : new PathOperations().fileToURI(analysisRun.directoryInfo.directoryPath)}};
               
        this.addFiles(analysisRun.files,runNumber);
        this.addResults(analysisRun.problems,analysisRun.directoryInfo.directoryPath,runNumber);
        this.addRules(analysisRun.rules,runNumber);

    }

    /**
     * Add all of the rules from this analysis run to the Sarif object that will be output (Goes into runs[runNumber].tool.driver.rules in the output)
     * @param rules array of all of the rules loaded.  The settings that the overall object was instantiated with in the constructor determine 
     * if the manual review and best practice rules are included
     * @param runNumber the run that these rules were used in
     */
    private addRules(rules : DevSkimObjects.Rule[], runNumber : number)
    {
        if(this.SarifFileObject.runs.length < runNumber)
        {
            throw "Run Object for this run has not yet been created";
        }

        this.SarifFileObject.runs[runNumber].tool.driver.rules = [];

        // Ensure that all rules are specified in a stable order. 
        const _rules: DevSkimObjects.Rule[] = rules.sort((a, b) => a.id.localeCompare(b.id));

        for(let rule of _rules)
        {
            //check if the optional rules were enabled in this run before adding the rule to the
            //sarif collection
            if((rule.severity != "best-practice" || this.devskimSettings.enableBestPracticeRules) &&
                (rule.severity != "manual-review" || this.devskimSettings.enableManualReviewRules))
            {
                let newSarifRule : SARIF21Schema.ReportingDescriptor = Object.create(null);
                newSarifRule.id = rule.id;
                newSarifRule.name = rule.name;
                newSarifRule.fullDescription = {"text" : rule.description};
                newSarifRule.helpUri = this.devskimSettings.guidanceBaseURL + rule.ruleInfo;
                switch(rule.severity)
                {
                    case "critical":
                    case "important":
                    case "moderate":    newSarifRule.defaultConfiguration = {"level": "error"};
                        break;
                    default: newSarifRule.defaultConfiguration = {"level": "note"};
                }
                //sarif doesn't have a field for the security severity, so put it in a property bag
                newSarifRule.properties = {"MSRC-severity": rule.severity};
                this.SarifFileObject.runs[runNumber].tool.driver.rules.push(newSarifRule);
            }
        }
    }

    /**
     * Add all of the files analyzed to the sarif output.  This goes in runs[runNumber].artifacts
     * @param files array of all of the files and their meta data that were analyzed
     * @param runNumber the run that these files were recorded from
     */
    private addFiles(files : DevSkimObjects.FileInfo[], runNumber : number)
    {
        if(this.SarifFileObject.runs.length < runNumber)
        {
            throw "Run Object for this run has not yet been created";
        }

        this.SarifFileObject.runs[runNumber].artifacts = [];

        for(let file of files)
        {
            let sarifFile : SARIF21Schema.Artifact = Object.create(null);
            sarifFile.location = Object.create(null);
            sarifFile.location.uri = file.fileURI;
            sarifFile.location.uriBaseId = "%SRCROOT%";
            sarifFile.length = file.fileSize;
            sarifFile.sourceLanguage = file.sourceLanguageSARIF;
            sarifFile.hashes = {"sha-256" : file.sha256hash, "sha-512": file.sha512hash};
            this.SarifFileObject.runs[runNumber].artifacts.push(sarifFile);
        }
    }

    /**
     * Add the results of the analysis to the sarif object.  Will populate runs[runNumber].results in the output
     * @param problems array of every finding from the analysis run
     * @param directory the parent directory these findings were found under
     * @param runNumber the run that these findings came from
     */
    private addResults(problems : DevSkimObjects.DevSkimProblem[], directory : string, runNumber : number)
    {
        if(this.SarifFileObject.runs.length < runNumber)
        {
            throw "Run Object for this run has not yet been created";
        }
                
        this.SarifFileObject.runs[runNumber].results = [];
        let pathOp : PathOperations = new PathOperations();

        for(let problem of problems)
        {
            let sarifResult : SARIF21Schema.Result = Object.create(null);
            sarifResult.ruleId = problem.ruleId;
            sarifResult.message = {"text" : problem.message};
            
            switch(problem.severity)
            {
                case DevSkimObjects.DevskimRuleSeverity.Critical:
                case DevSkimObjects.DevskimRuleSeverity.Important:
                case DevSkimObjects.DevskimRuleSeverity.Moderate:    sarifResult.level = "error";
                    break;
                default: sarifResult.level = "note";
            }
            sarifResult.locations = [];
            sarifResult.locations[0] = Object.create(null);
            sarifResult.locations[0].physicalLocation = Object.create(null);

            let filePath = pathOp.fileToURI(problem.filePath );
            filePath = filePath.substr(pathOp.fileToURI(directory).length+1);

            sarifResult.locations[0].physicalLocation.artifactLocation = {"uri" : filePath, "uriBaseId" : "%SRCROOT%", "sourceLanguage" : pathOp.getLangFromPath(problem.filePath, true)};
            sarifResult.locations[0].physicalLocation.region = Object.create(null);

            //LSP uses 0 indexed lines/columns, SARIF expects 1 indexed, hence the + 1
            sarifResult.locations[0].physicalLocation.region.startLine = problem.range.start.line + 1;
            sarifResult.locations[0].physicalLocation.region.endLine = problem.range.end.line + 1;

            sarifResult.locations[0].physicalLocation.region.startColumn = problem.range.start.character + 1;
            sarifResult.locations[0].physicalLocation.region.endColumn = problem.range.end.character + 1;
            if(problem.snippet && problem.snippet.length > 0)
            {
                sarifResult.locations[0].physicalLocation.region.snippet = {"text" : problem.snippet};
            }
            this.SarifFileObject.runs[runNumber].results.push(sarifResult);
        }
    }

    /**
     * Generate the output string that will either be written to the console or to a file by writeOutput
     * the base implementation for writeOutput calls the function and then writes out to the given location
     * so all that is necessary is defining the output to be written
     */    
    protected createOutput(): string
    {
        return JSON.stringify(this.SarifFileObject , null, 4);
    }    
}