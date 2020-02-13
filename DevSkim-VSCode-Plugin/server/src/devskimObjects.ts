/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * 
 * This file contains the object and enumeration definitions for DevSkim Rules (the logic to 
 * find an issue), Problems (a specific part of the code in which a rule triggered on), and 
 * fixes for problems (instructions to VS Code on how to transform problematic code into safe code)
 * 
 *  ------------------------------------------------------------------------------------------ */

import
{
	Diagnostic, DiagnosticSeverity, Range,
} from 'vscode-languageserver';

import { DevSkimWorkerSettings } from "./devskimWorkerSettings";
import {GitRepoInfo } from 'git-repo-info';

/**
 * A collection of data about a run in a folder containing .git info and all the files under it.
 * If DevSkim was run on a folder structure without .git info, there should only be one run with the top level folder
 * Used in the CLI but not the IDE
 */
export class Run
{
     /**
      * Create a run object
      * @param directoryInfo Info for the highest level directory this analysis took place in
      * @param rules the active rules used in this analysis run
      * @param files all the files scanned by the run, even if no issues were detected
      * @param problems all of the findings from this run
      */
    constructor(public directoryInfo : DirectoryInfo, 
                public rules : Rule[], 
                public files : FileInfo[],
                public problems : DevSkimProblem[]  )
    {
        
    }
}



/**
 * Class representing the Tool information, corresponding to the SARIF Tool.Driver object
 */
export class ToolVersion
{
	/**
	 * common name for the tool
	 */
	public name : string = "DevSkim";

	/**
	 * Name, plus version and any notes
	 */
	public fullName : string = "DevSkim Security Analyzer, version 0.3.0 (Preview)";

	/**
	 * Quick description meant to show up in listings and tables of tools
	 */
	public shortDescription : IToolDescriptor = {"text": "Lightweight Security Linter"};

	/**
	 * more comprehensive name
	 */
	public fullDescription : IToolDescriptor = {"text": "Lightweight security linter capable of finding common security mistakes across a variety of languages without needing to compile."};

	/**
	 * Different formats of the version
	 */
	public version : string = "0.3";
	public semanticVersion : string = "0.3.0";
	public dottedQuadFileVersion : string = "0.3.0.0";

	/**
	 * Publishing information
	 */
	public organization : string = "Microsoft DevLabs";    	
}

/**
 * SARIF multi-format descriptor interface, to support describing something in both text, and formatted markdown
 */
export interface IToolDescriptor
{
	/**
	 * plaintext description
	 */
	text : string;

	/**
	 * (optional) Markdown formatted description
	 */
	markdown ? : string;
}


/**
 * These are the example settings defined in the client's package.json
 */
export interface IDevSkimSettings
{
	/**
	 * Turn on the rules of severity "Best-Practice". These rules either flag issues that are typically of a 
	 * lower severity, or recommended practices that lead to more secure code, but aren't typically outright 
	 * vulnerabilities.
	 */
	enableBestPracticeRules: boolean;

	/**
	 * Turn on the rules that flag things for manual review. These are typically scenarios that *could* be 
	 * incredibly severe if tainted data can be inserted, but are often programmatically necessary 
	 * (for example, dynamic code generation with "eval").  Since these rules tend to require further 
	 * analysis upon flagging an issue, they disabled by default.
	 */
	enableManualReviewRules: boolean;	

	/**
	 * Each finding has a guidance file that describes the issue and solutions in more detail.  By default, 
	 * those files live on the DevSkim github repo however, with this setting, organizations can clone and 
	 * customize that repo, and specify their own base URL for the guidance.
	 */
	guidanceBaseURL: string;

	/**
	 * List of Files, File Extensions, and directories to ignore. *-based wild cards can be used. 
	 * Directories should be specified with forward slash (/) 
	 */	
	ignoreFiles: string[];

	/**
	 * To Do - replace with run-only and exclude
	 */
	ignoreRulesList: string[];
	
	/**
	 * Intended to help while authoring new rules. When loading the DevSkim rules, analyze the JSON files that 
	 * contain the rules for mistakes.  If the mistakes are correctable, DevSkim will create a Fixed-Rules folder 
	 * containing the corrected rules within its extension directory.  rulesValidationLog.json is also created in 
	 * the DevSkim directory logging any issues found.  This setting is off by default, to reduce load time.
	 */
	validateRulesFiles: boolean;

	/**
	 * enable debug message to either the console or the remote console if running in vs code debugging
	 */
	debugLogging : boolean;

	/**
	 * To control the performance of DevSkim, this setting controls the maximum file size that it will analyze, 
	 * in Kilobytes.
	 */
	maxFileSizeKB : number;

	//--------------------------------------
	//IDE Only

	/**
	 * when enableManualReviewRules is set to true, if this property is set to a name/alias/email address
	 * it will be inserted when marking a finding as reviewed.  
	 * For example //DevSkim: reviewed DS123321 on 2016-12-10 by <whatever this value is>.  
	 * If left blank the "by <whatever this value is>" is omitted when marking a finding reviewed
	 */
	manualReviewerName: string;

	/**
	 * By default, when a source file is closed the findings remain in the 'Problems' window.  
	 * Setting this value to true will cause findings to be removed from 'Problems' when the document is closed.  
	 * Note, setting this to true will cause findings that are listed when invoking the 'Scan all files in workspace' 
	 * command to automatically clear away after a couple of minutes
	 */
	removeFindingsOnClose: boolean;

	/**
	 * DevSkim allows for findings to be suppressed for a temporary period of time. The default is 30 days.  
	 * Set to 0 to disable temporary suppressions.
	 */
	suppressionDurationInDays: number;

	/**
	 * When DevSkim inserts a suppression comment it defaults to using single line comments for every language 
	 * that has them.  Setting this to block will instead use block comments for the languages that support them.  
	 * Block comments are suggested if regularly adding explanations for why a finding was suppressed.
	 * 
	 * values are "line" or "block"
	 */
	suppressionCommentStyle: string;

	/**
	 * When DevSkim inserts a suppression comment it defaults placing the comment after the finding being suppressed, 
	 * on the same line.  Changing this setting will place the suppression the line above the finding instead.
	 * 
	 * values are "same line as finding" or "line above finding"
	 */
	suppressionCommentPlacement: string;
	
	//--------------------------------------
	//Internal use

	/**
	 * Information about the version of DevSkim being run
	 */
	toolInfo : ToolVersion;
}



/**
 * Details of the file being analyzed
 */
export interface FileInfo
{
	/**
	 * Location of the file being analyzed
	 */
	fileURI : string; 

	/**
	 * Source Language of the file being analyzed, using SARIF conventions
	 */
	sourceLanguageSARIF : string;

	/**
	 * The size of the file in bytes
	 */
	fileSize : number;

	/**
	 * SHA 256 hash of the file, for fingerprinting exact version of the file
	 */
	sha256hash : string;

	/**
	 * SHA512 hash of the file, for fingerprinting exact version of the file
	 */
	sha512hash : string;
}


/**
 * Object to capture the information about a directory being analyzed.  Currently
 * only used when running the devskim command line
 */
export interface DirectoryInfo
{
	/**
	 * directory being analyzed
	 */
	directoryPath : string;

	/**
	 * (optional) the git repo that populated the directory, if present
	 */
	gitRepo ?: string;

	/**
	 * (optional, but should be populated if gitRepo is) additional git information such as
	 * branch, sha of commit, etc.
	 */
	gitInfo ?: GitRepoInfo;
}


/**
 * An Interface corresponding to the Pattern section of the JSON
 * rules files.  The pattern is used to match a problem within the source
 * 
 * @export
 * @interface Pattern
 */
export interface Pattern
{
	pattern: string;
	type: string;
	modifiers?: string[];
	scopes?: string[];
	_comment?: string;
}


/**
 * An Interface corresponding to the lambda section of the JSON
 * rules files.  This object can only be used within conditions, and
 * is an alternative to pattern
 * 
 * @export
 * @interface Lambda
 */
export interface Lambda
{
	lambda_code: string;
	_comment?: string;
}


/**
 * An Interface corresponding to the FixIt section of the JSON
 * rules files.  The FixIt contains the instructions to translate a flagged piece
 * of code into a preferred alternative
 * 
 * @export
 * @interface FixIt
 */
export interface FixIt
{
	type: string;
	name: string;
	pattern: Pattern;
	replacement: string;
	_comment?: string;
}


/**
 * An Interface corresponding to an individual rule within the JSON
 * rules files.  The rule definition includes how to find the problem (the patterns),
 * description of the problem, text on how to fix it, and optionally an automated fix ( Fix_it)
 * 
 * @export
 * @interface Rule
 */
export interface Rule
{
	id: string;
	overrides?: string[];
	name: string;
	active: boolean;
	tags: string[];
	applies_to?: string[];
	severity: string;
	description: string;
	recommendation: string;
	ruleInfo: string;
	patterns: Pattern[];
	conditions?: Condition[];
	fix_its?: FixIt[];
	filepath?: string; //filepath to the rules file the rule came from
	_comment?: string;
}


export interface Condition
{
	pattern: Pattern;
	lambda: Lambda;
	search_in: string;
	_comment?: string;
	negateFinding?: boolean;

}


/**
 * A Key/Object collection, used to associate a particular fix with a diagnostic and the file it is located in
 * 
 * @export
 * @interface Map
 * @template V
 */
export interface Map<V>
{
	[key: string]: V;
}


/**
 * An object to represent a fix at a particular line of code, including which revision of a file it applies to
 * 
 * @export
 * @interface AutoFix
 */
export interface AutoFix
{
	label: string;
	documentVersion: number;
	ruleId: string;
	edit: DevSkimAutoFixEdit;
}

/**
 * the specific technical details of a fix to apply 
 * 
 * @export
 * @interface DevSkimAutoFixEdit
 */
export interface DevSkimAutoFixEdit
{
	range: Range;
	fixName?: string;
	text: string;
}


/**
 * The nomenclature for DevSkim severities is based on the MSRC bug bar.  There are  
 * many different severity ranking systems and nomenclatures in use, and no clear "best"
 * so since this project was started by Microsoft employees the Microsoft nomenclature was
 * chosen
 * 
 * @export
 * @enum {number}
 */
export enum DevskimRuleSeverity
{
	Critical,
	Important,
	Moderate,
	BestPractice,
	WarningInfo,
	ManualReview,
	// this isn't actually an error level in rules, but used when flagging
	// DS identifiers in suppression and other comments
}

/**
 * A class to represent a finding at a particular line of code
 * 
 * @export
 * @class DevSkimProblem
 */
export class DevSkimProblem
{
	public range: Range;
	public source: string;
	public severity: DevskimRuleSeverity;
	public ruleId: string; //the id in the rules JSON files
	public message: string; //a description of the problem
	public issueURL: string; //url for more info on the issue
	public replacement: string; //text on how to deal with the problem, intended to guide the user
	public fixes: DevSkimAutoFixEdit[]; //fixes for the issue discovered
	public suppressedFindingRange: Range; //if there is a suppression comment, the range for that comment
	public filePath: string; //the location of the file the finding was discovered in
	public overrides: string[]; //a collection of ruleIDs that this rule supersedes
	public snippet: string; //the offending code snippet that the problem is located in

    /**
     * Creates an instance of DevSkimProblem.
     * 
     * @param {string} message guidance to display for the problem (description in the rules JSON)
     * @param {string} source the name of the rule that was triggered (name in the rules JSON)
     * @param {string} ruleId a unique identifier for that particular rule (id in the rules JSON)
     * @param {string} severity MSRC based severity for the rule - Critical, Important, Moderate, Low, Informational (severity in rules JSON)
     * @param replacement @todo update this
     * @param {string} issueURL a URL to some place the dev can get more information on the problem (rules_info in the rules JSON)
     * @param {Range} range where the problem was found in the file (line start, column start, line end, column end) 
     */
	constructor(message: string, source: string, ruleId: string, severity: DevskimRuleSeverity, replacement: string, issueURL: string, range: Range, snippet : string)
	{
		this.fixes = [];
		this.overrides = [];
		this.message = (message !== undefined && message.length > 0) ? message : "";
		this.source = (source !== undefined && source.length > 0) ? source : "";
		this.ruleId = (ruleId !== undefined && ruleId.length > 0) ? ruleId : "";
		this.issueURL = (issueURL !== undefined && issueURL.length > 0) ? issueURL : "";
		this.replacement = (replacement !== undefined && replacement.length > 0) ? replacement : "";
		this.range = (range !== undefined) ? range : Range.create(0, 0, 0, 0);
		this.severity = severity;
		this.suppressedFindingRange = null;
		this.snippet = snippet;
	}

	/**
	 * Shorten the severity name for output
	 * 
	 * @param {DevskimRuleSeverity} severity the current enum value for the severity we are converting
	 * @returns {string} short name of the severity rating
	 * 
	 * @memberOf DevSkimProblem
	 */
	public static getSeverityName(severity: DevskimRuleSeverity): string
	{
		switch (severity)
		{
			case DevskimRuleSeverity.Critical: return "[Critical]";
			case DevskimRuleSeverity.Important: return "[Important]";
			case DevskimRuleSeverity.Moderate: return "[Moderate]";
			case DevskimRuleSeverity.ManualReview: return "[Review]";
			default: return "[Best Practice]";
		}
	}

    /**
     * Converts the MSRC based rating (Critical, Important, Moderate, Low, Informational) into a VS Code Warning level
     * Critical/Important get translated as Errors, and everything else as a Warning
     * 
     * @returns {DiagnosticSeverity}
     */
	public getWarningLevel(): DiagnosticSeverity
	{
		//mark any optional rule, or rule that is simply informational as a warning (i.e. green squiggle)
		switch (this.severity)
		{
			case DevskimRuleSeverity.WarningInfo:
			case DevskimRuleSeverity.ManualReview: return DiagnosticSeverity.Information;

			case DevskimRuleSeverity.BestPractice: return DiagnosticSeverity.Warning;

			case DevskimRuleSeverity.Moderate:
			case DevskimRuleSeverity.Important:
			case DevskimRuleSeverity.Critical:
			default: return DiagnosticSeverity.Error;
		}
	}

    /**
     * Make a VS Code Diagnostic object from the information in this DevSkim problem
	 * @param dswSettings the current settings analysis is being run under
     * 
     * @returns {Diagnostic} the diagnostic for the current DevSKim problem
     */
	public makeDiagnostic(dswSettings: DevSkimWorkerSettings): Diagnostic
	{
		const diagnostic: Diagnostic = Object.create(null);
		let fullMessage =
			`${this.source}\nSeverity: ${DevSkimProblem.getSeverityName(this.severity)}\n\n${this.message}`;

		fullMessage = (this.replacement.length > 0) ?
			fullMessage + "\n\nFix Guidance: " + this.replacement :
			fullMessage;

		fullMessage = (this.issueURL.length > 0) ?
			fullMessage + "\n\nMore Info:\n" + dswSettings.getSettings().guidanceBaseURL + this.issueURL + "\n" :
			fullMessage;

		diagnostic.message = fullMessage;
		diagnostic.code = this.ruleId;
		diagnostic.source = "Devskim: Finding " + this.ruleId;
		diagnostic.range = this.range;
		diagnostic.severity = this.getWarningLevel();

		return diagnostic;
	}
}

/**
 * this creates a unique key for a diagnostic & code fix combo (i.e. two different code fixes for the same diagnostic get different keys)
 * used to correlate a code fix with the line of code it is supposed to fix, and the problem it should fix
 * 
 * @export
 * @param {Range} range the location of an issue within a document
 * @param {number} diagnosticCode the code value in a Diagnostic, or similar numeric ID
 * @returns {string} a unique key identifying a diagnostics+fix combination
 */
export function computeKey(range: Range, diagnosticCode: string | number): string
{
	return `[${range.start.line},${range.start.character},${range.end.line},${range.end.character}]-${diagnosticCode}`;
}


/**
 * Class of Code Fixes corresponding to a line of code
 * 
 * @export
 * @class Fixes
 */
export class Fixes
{
	private keys: string[];

	constructor(private edits: Map<AutoFix>)
	{
		this.keys = Object.keys(edits);
	}

	public static overlaps(lastEdit: AutoFix, newEdit: AutoFix): boolean
	{
		return !!lastEdit && lastEdit.edit.range[1] > newEdit.edit.range[0];
	}

	public isEmpty(): boolean
	{
		return this.keys.length === 0;
	}

	public getDocumentVersion(): number
	{
		return this.edits[this.keys[0]].documentVersion;
	}

	public getScoped(diagnostics: Diagnostic[]): AutoFix[]
	{
		let result: AutoFix[] = [];
		for (let diagnostic of diagnostics)
		{
			let key = computeKey(diagnostic.range, diagnostic.code);
			let x = 0;
			let editInfo: AutoFix = this.edits[key + x.toString(10)];
			while (editInfo)
			{
				result.push(editInfo);
				x++;
				editInfo = this.edits[key + x.toString(10)];
			}
		}
		return result;
	}

	public getAllSorted(): AutoFix[]
	{
		let result = this.keys.map(key => this.edits[key]);
		return result.sort((a, b) =>
		{
			let d = a.edit.range[0] - b.edit.range[0];
			if (d !== 0)
			{
				return d;
			}
			if (a.edit.range[1] === 0)
			{
				return -1;
			}
			if (b.edit.range[1] === 0)
			{
				return 1;
			}
			return a.edit.range[1] - b.edit.range[1];
		});
	}

	public getOverlapFree(): AutoFix[]
	{
		let sorted = this.getAllSorted();
		if (sorted.length <= 1)
		{
			return sorted;
		}
		let result: AutoFix[] = [];
		let last: AutoFix = sorted[0];
		result.push(last);
		for (let i = 1; i < sorted.length; i++)
		{
			let current = sorted[i];
			if (!Fixes.overlaps(last, current))
			{
				result.push(current);
				last = current;
			}
		}
		return result;
	}
}






