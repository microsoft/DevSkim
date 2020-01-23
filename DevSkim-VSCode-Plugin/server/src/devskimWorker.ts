/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * 
 * This file contains the actual meat and potatoes of analysis.  The DevSkimWorker class does 
 * the actual work of analyzing data it was given
 * 
 * Most of the type declarations representing things like the rules used to analyze a file, and 
 * problems found in a file, are in devskimObjects.ts
 * 
 * ------------------------------------------------------------------------------------------ */
import { Range } from 'vscode-languageserver';
import 
{
    computeKey, Condition, DevSkimProblem, DevskimRuleSeverity, Map, AutoFix,
    Rule, DevSkimAutoFixEdit, IDevSkimSettings,
}    from "./devskimObjects";

import { DevSkimSuppression, DevSkimSuppressionFinding } from "./utility_classes/suppressions";
import { PathOperations } from "./utility_classes/pathOperations";
import { DevSkimWorkerSettings } from "./devskimWorkerSettings";
import { RulesLoader } from "./utility_classes/rulesLoader";
import {DevskimLambdaEngine} from "./devskimLambda";
import {DocumentUtilities} from "./utility_classes/document";
import { DebugLogger } from "./utility_classes/logger";

/**
 * The bulk of the DevSkim analysis logic.  Orchestrates Loading rules in, implements and exposes functions to run rules across a file
 */
export class DevSkimWorker 
{
    public dswSettings: DevSkimWorkerSettings = new DevSkimWorkerSettings();
    public readonly rulesDirectory: string;
    private analysisRules: Rule[];

    //codeActions is the object that holds all of the autofix mappings. we need to store them because
    //the CodeActions are created at a different point than the diagnostics, yet we still need to be able
    //to associate the action with the diagnostic.  So we create a mapping between them and look up the fix
    //in the map using values out of the diagnostic as a key
    //
    //We use nested Maps to store the fixes.  The key to the first map is the document URI.  This maps a 
    //specific file to a map of the fixes for that file.  The key for this second map is created in
    //the devskimObjects.ts file, in the function computeKey.  the second key is in essence a combination of
    //a diagnostic and a string representation of a number for a particular fix, as there may be multiple fixes associated with a single diagnostic
    //i.e. we suggest both strlcpy and strcpy_s to fix strcpy
    //
    //it's format is essentially <document URI <diagnostic + fix#>>.  We could instead have done <document <diagnostic <fix#>>>, but three deep
    //map seemed a little excessive to me.  Then again, I just wrote 3 paragraphs for how this works, so maybe I'm being too clever
    public codeActions: Map<Map<AutoFix>> = Object.create(null);

    /**
     * Instantiate the DevSkim Worker
     * @param logger a logger object, to decide where messages should be written (console, remote console, nowhere, etc)
     * @param dsSuppressions an existing suppressions object to hold information about suppressed findings
     * @param settings the settings analysis should run under (can be updated post instantiate with UpdateSettings method)
     */
    constructor(private logger: DebugLogger, private dsSuppressions: DevSkimSuppression, settings: IDevSkimSettings = DevSkimWorkerSettings.defaultSettings()) 
    {
        this.rulesDirectory = DevSkimWorkerSettings.getRulesDirectory(logger);
        this.dswSettings.setSettings(settings);
        this.dsSuppressions.dsSettings = settings;
    }

    /**
     * Call whenever the user updates their settings in the IDE, to ensure that the worker is using the most up to date
     * version of the user's settings
     * @param settings the current settings to update
     */
    public UpdateSettings(settings : IDevSkimSettings)
    {
        this.dswSettings.setSettings(settings);
        this.dsSuppressions.dsSettings = settings;
        
    }

    /**
     * Must be called before analysis is effective.  Loads the rules from the file system
     */
    public async init(): Promise<void> 
    {
        await this.loadRules();
    }

    /**
     * Look for problems in the provided text.  For the IDE includeSuppressions should be true so that users see details
     * about what rule was suppressed in a suppression comment.  When called from teh CLI the value should be false, so that
     * it doesn't get included in the output
     *
     * @param {string} documentContents the contents of a file to analyze
     * @param {string} langID the programming language for the file
     * @param {string} documentURI the URI identifying the file
     * @param {boolean} includeSuppressions true if the resulting problems should include information squiggles for the ruleID listed in a suppression comment
     * @returns {DevSkimProblem[]} an array of all of the issues found in the text
     */
    public analyzeText(documentContents: string, langID: string, documentURI: string, includeSuppressions : boolean = true): DevSkimProblem[] 
    {
        let problems: DevSkimProblem[] = [];

        //Before we do any processing, see if the file (or its directory) are in the ignore list.  If so
        //skip doing any analysis on the file
        if (this.analysisRules && this.analysisRules.length
            && this.dswSettings && this.dswSettings.getSettings().ignoreFiles
            && !PathOperations.ignoreFile(documentURI, this.dswSettings.getSettings().ignoreFiles)
            && documentContents.length < this.dswSettings.getSettings().maxFileSizeKB * 1024) 
        {

            //find out what issues are in the current document
            problems = this.runAnalysis(documentContents, langID, documentURI, includeSuppressions);

            //remove any findings from rules that have been overridden by other rules
            problems = this.processOverrides(problems);
        }
        return problems;
    }

    /**
     * Save a codeAction for a particular auto-fix to the codeActions map, so that it can be looked up when onCodeAction is called
     * and actually communicated to the VSCode engine.  Since creating a diagnostic and assigning a code action happen at different points
     * its important to be able to look up what code actions should be populated at a given time
     *
     * @param {string} documentURI the path to the document, identifying it
     * @param {number} documentVersion the current revision of the document (vs code calculates this)
     * @param range  @ToDo: update this document
     * @param {string | number} diagnosticCode the diagnostic a fix is associated with
     * @param {DevSkimAutoFixEdit} fix the actual data about the fix being applied (location, name, action, etc.)
     * @param {string} ruleID an identifier for the rule that was triggered
     * @returns {void}
     */
    public recordCodeAction(documentURI: string, documentVersion: number, range: Range, diagnosticCode: string | number, fix: DevSkimAutoFixEdit, ruleID: string): void 
    {
        if (!fix || !ruleID) 
        {
            return;
        }
        let fixName: string = (fix.fixName !== undefined && fix.fixName.length > 0) ? fix.fixName : `Fix this ${ruleID} problem`;
        let edits: Map<AutoFix> = this.codeActions[documentURI];
        if (!edits) 
        {
            edits = Object.create(null);
            this.codeActions[documentURI] = edits;
        }

        let x = 0;
        //figure out how many existing fixes are associated with a given diagnostic by checking if it exists, and incrementing until it doesn't
        while (edits[computeKey(range, diagnosticCode) + x.toString(10)]) 
        {
            x++;
        }

        //create a new mapping, using as the key the diagnostic the fix is associated with and a number representing whether this is the 1st fix
        //to associate with that diagnostic, 2nd, 3rd, and so on.  This lets us map multiple fixes to one diagnostic while providing an easy way
        //to iterate.  we could have instead made this a three nested map <file<diagnostic<fix#>>> but this achieves the same thing 
        edits[computeKey(range, diagnosticCode) + x.toString(10)] =
            {
                label: fixName,
                documentVersion: documentVersion,
                ruleId: ruleID,
                edit: fix,
            };
    }

    /**
     * Reload the rules from the file system.  Since this right now is just a proxy for loadRules this *could* have been achieved by
     * exposing loadRules as public.  I chose not to, as eventually it might make sense here to check if an analysis is actively running
     * and hold off until it is complete.  I don't forsee that being an issue when analyzing an individual file (it's fast enough a race condition
     * should exist with reloading rules), but might be if doing a full analysis of a lot of files.  So in anticipation of that, I broke this
     * into its own function so such a check could be added.
     */
    public async refreshAnalysisRules(): Promise<void> 
    {
        return this.loadRules();
    }

    /**
     * Return the collection of rules currently loaded into the analysis engine
     */
    public retrieveLoadedRules() : Rule[]
    {
        return this.analysisRules;
    }

    /**
     * recursively load all of the JSON files in the $userhome/.vscode/extensions/vscode-devskim/rules sub directories
     *
     * @private
     */
    private async loadRules(): Promise<void> 
    {
        const loader = new RulesLoader(this.logger, true, this.rulesDirectory);
        const rules = await loader.loadRules();
        this.analysisRules = await loader.validateRules(rules)
    }

    /**
     * Best practice and Manual Review severity rules may be turned on and off via a setting
     * prior to running an analysis, verify that the rule is enabled based on its severity and the user settings
     *
     * @public
     * @param {DevskimRuleSeverity} ruleSeverity the severity of the current rule
     * @returns {boolean} true if it should be processed (its either a high severity or the severity is enabled in settings)
     *
     * @memberOf DevSkimWorker
     */
    public RuleSeverityEnabled(ruleSeverity: DevskimRuleSeverity): boolean 
    {
        return ruleSeverity == DevskimRuleSeverity.Critical ||
            ruleSeverity == DevskimRuleSeverity.Important ||
            ruleSeverity == DevskimRuleSeverity.Moderate ||
            (ruleSeverity == DevskimRuleSeverity.BestPractice &&
                this.dswSettings.getSettings().enableBestPracticeRules == true) ||
            (ruleSeverity == DevskimRuleSeverity.ManualReview &&
                this.dswSettings.getSettings().enableManualReviewRules == true);

    }

    /**
     * maps the string for severity received from the rules into the enum (there is inconsistencies with the case used
     * in the rules, so this is case incentive).  We convert to the enum as we do comparisons in a number of places
     * and by using an enum we can get a transpiler error if we remove/change a label
     *
     * @public
     * @param {string} severity the text severity from the rules JSON
     * @returns {DevskimRuleSeverity} the enum used in code for the severity, corresponding to the text
     *
     * @memberOf DevSkimWorker
     */
    public static MapRuleSeverity(severity: string): DevskimRuleSeverity 
    {
        switch (severity.toLowerCase())
        {
            case "critical":
                return DevskimRuleSeverity.Critical;
            case "important":
                return DevskimRuleSeverity.Important;
            case "moderate":
                return DevskimRuleSeverity.Moderate;
            case "best-practice":
                return DevskimRuleSeverity.BestPractice;
            case "manual-review":
                return DevskimRuleSeverity.ManualReview;
            default:
                return DevskimRuleSeverity.BestPractice;
        }
    }

    /**
     * the pattern type governs how we form the regex.  regex-word is wrapped in \b, string is as well, but is also escaped.
     * substring is not wrapped in \b, but is escaped, and regex/the default behavior is a vanilla regular expression
     * @param {string} regexType regex|regex-word|string|substring
     * @param {string} pattern the regex pattern from the Rules JSON
     * @param {string[]} modifiers modifiers to use when creating regex. can be null.  a value of "d" will be ignored if forXregExp is false
     * @param {boolean} forXregExp whether this is for the XRegExp regex engine (true) or the vanilla javascript regex engine (false)
     */
    public static MakeRegex(regexType: string, pattern: string, modifiers: string[], forXregExp: boolean): RegExp 
    {
        //create any regex modifiers
        let regexModifier = "";
        if (modifiers != undefined && modifiers) 
        {
            for (let mod of modifiers) 
            {
                //xregexp implemented dotmatchall as s instead of d
                if (mod == "d") 
                {
                    //also, Javascript doesn't support dotmatchall natively, so only use this if it will be used with XRegExp
                    if (forXregExp) 
                    {
                        regexModifier = regexModifier + "s";
                    }
                }
                else 
                {
                    regexModifier = regexModifier + mod;
                }
            }
        }

        //now create a regex based on the 
        let XRegExp = require('xregexp');
        switch (regexType.toLowerCase()) 
        {
            case 'regex-word':
                return XRegExp('\\b' + pattern + '\\b', regexModifier);
            case 'string':
                return XRegExp('\\b' + XRegExp.escape(pattern) + '\\b', regexModifier);
            case 'substring':
                return XRegExp(XRegExp.escape(pattern), regexModifier);
            default:
                return XRegExp(pattern, regexModifier);
        }
    }

    /**
     * Perform the actual analysis of the text, using the provided rules
     *
     * @param {string} documentContents the full text to analyze
     * @param {string} langID the programming language for the text
     * @param {string} documentURI URI identifying the document
     * @param {boolean} includeSuppressions true if the resulting problems should include information squiggles for the ruleID listed in a suppression comment
     * @returns {DevSkimProblem[]} all of the issues identified in the analysis
     */
    private runAnalysis(documentContents: string, langID: string, documentURI: string, includeSuppressions : boolean = true): DevSkimProblem[] 
    {
        let problems: DevSkimProblem[] = [];
        let XRegExp = require('xregexp');

        //iterate over all of the rules, and then all of the patterns within a rule looking for a match.
        for (let rule of this.analysisRules)
        {
            const ruleSeverity: DevskimRuleSeverity = DevSkimWorker.MapRuleSeverity(rule.severity);
            //if the rule doesn't apply to whatever language we are analyzing (C++, Java, etc.) or we aren't processing
            //that particular severity skip the rest
            if (this.dswSettings.getSettings().ignoreRulesList.indexOf(rule.id) == -1 &&  /*check to see if this is a rule the user asked to ignore */
                DevSkimWorker.appliesToLangOrFile(langID, rule.applies_to, documentURI) &&
                this.RuleSeverityEnabled(ruleSeverity)) 
            {
                for (let patternIndex = 0; patternIndex < rule.patterns.length; patternIndex++) 
                {
                    let modifiers: string[] = (rule.patterns[patternIndex].modifiers != undefined && rule.patterns[patternIndex].modifiers.length > 0) ?
                        rule.patterns[patternIndex].modifiers.concat(["g"]) : ["g"];

                    const matchPattern: RegExp = DevSkimWorker.MakeRegex(rule.patterns[patternIndex].type, rule.patterns[patternIndex].pattern, modifiers, true);

                    //go through all of the text looking for a match with the given pattern
                    let matchPosition = 0;
                    let match = XRegExp.exec(documentContents, matchPattern, matchPosition);
                    while (match) 
                    {
                        //if the rule doesn't contain any conditions, set it to an empty array to make logic later easier
                        if (!rule.conditions) 
                        {
                            rule.conditions = [];
                        }

                        //check to see if this finding has either been suppressed or reviewed (for manual-review rules)
                        //the suppressionFinding object contains a flag if the finding has been suppressed as well as
                        //range info for the ruleID in the suppression text so that hover text can be added describing
                        //the finding that was suppress
                        let suppressionFinding: DevSkimSuppressionFinding = DevSkimSuppression.isFindingCommented(match.index, documentContents, rule.id,langID, (ruleSeverity == DevskimRuleSeverity.ManualReview));

                        //calculate what line we are on by grabbing the text before the match & counting the newlines in it
                        let lineStart: number = DocumentUtilities.GetLineNumber(documentContents, match.index);
                        let newlineIndex: number = (lineStart == 0) ? -1 : documentContents.substr(0, match.index).lastIndexOf("\n");
                        let columnStart: number = match.index - newlineIndex - 1;

                        //since a match may span lines (someone who broke a long function invocation into multiple lines for example)
                        //it's necessary to see if there are any newlines WITHIN the match so that we get the line the match ends on,
                        //not just the line it starts on.  Also, we use the substring for the match later when making fixes
                        let replacementSource: string = documentContents.substr(match.index, match[0].length);
                        let lineEnd: number = DocumentUtilities.GetLineNumber(replacementSource, replacementSource.length) + lineStart;

                        let columnEnd = (lineStart == lineEnd) ?
                            columnStart + match[0].length :
                            match[0].length - documentContents.substr(match.index).indexOf("\n") - 1;

                        let range: Range = Range.create(lineStart, columnStart, lineEnd, columnEnd);

                        //look for the suppression comment for that finding
                        if (!suppressionFinding.showSuppressionFinding &&
                            DocumentUtilities.MatchIsInScope(langID, documentContents.substr(0, match.index), newlineIndex, rule.patterns[patternIndex].scopes) &&
                            DevSkimWorker.MatchesConditions(rule.conditions, documentContents, range, langID)) 
                        {
                            let snippet = [];
                            for (let i=Math.max(0, lineStart - 2); i<=lineEnd + 2; i++)
                            {
                                const snippetLine = DocumentUtilities.GetLine(documentContents, i);
                                snippet.push(snippetLine.substr(0, 80));
                            }

                            //add in any fixes
                            let problem: DevSkimProblem = this.MakeProblem(rule, DevSkimWorker.MapRuleSeverity(rule.severity), range, snippet.join('\n'));
                            problem.fixes = problem.fixes.concat(DevSkimWorker.MakeFixes(rule, replacementSource, range));
                            problem.fixes = problem.fixes.concat(this.dsSuppressions.createActions(rule.id, documentContents, match.index, lineStart, langID, ruleSeverity));
                            problem.filePath = documentURI;
                            problems.push(problem);
                        }
                        //throw a pop up if there is a review/suppression comment with the rule id, so that people can figure out what was
                        //suppressed/reviewed
                        else if (!suppressionFinding.noRange && includeSuppressions) 
                        {
                            //highlight suppression finding for context
                            //this will look
                            let problem: DevSkimProblem = this.MakeProblem(rule, DevskimRuleSeverity.WarningInfo, suppressionFinding.suppressionRange,"", range);

                            problems.push(problem);

                        }
                        //advance the location we are searching in the line
                        matchPosition = match.index + match[0].length;
                        match = XRegExp.exec(documentContents, matchPattern, matchPosition);
                    }
                }
            }
        }
        return problems;
    }



    /**
     * There are two conditions where this function gets called.  The first is to mark the code a rule triggered on and
     * in that case the rule, the severity of that rule, and the range of code for a specific finding found by that rule are
     * passed in.  suppressedFindingRange is ignored
     *
     * The second instance is when decorating the ruleID in a suppression or review comment.  e.g.:
     *     //DevSkim ignore: DS123456 or //DevSkim reviewed:DS123456
     * DevSkim will create a problem to mark the DS123456 so that when moused over so other people looking through the code
     * know what was suppressed or reviewed.  In this instance we still pass in the rule.  a Rule severity of warningInfo should
     * be passed in for warningLevel.  problemRange should be the range of the "DSXXXXXX" text that should get the information squiggle
     * and suppressedFindingRange should be the range of the finding that was suppressed or reviewed by the comment.  This last
     * is important, as we need to save that info for later to cover overrides that also should be suppressed
     * @param {Rule} rule the DevSkim rule that triggered on the problem
     * @param {DevskimRuleSeverity} warningLevel Error/Warning/Informational, corresponding to the IDE squiggle UI
     * @param {Range} problemRange the area that should get a squiggle added in the IDE
     * @param {string} snippet the text code snippet being flagged
     * @param {Range} [suppressedFindingRange] (optional) when creating a suppression squiggle, it gets a special range signifier
     */
    public MakeProblem(rule: Rule, warningLevel: DevskimRuleSeverity, problemRange: Range, snippet: string, suppressedFindingRange?: Range): DevSkimProblem
    {
        let problem: DevSkimProblem = new DevSkimProblem(rule.description, rule.name,
            rule.id, warningLevel, rule.recommendation, rule.ruleInfo, problemRange, snippet);

        if (suppressedFindingRange) 
        {
            problem.suppressedFindingRange = suppressedFindingRange;
        }

        if (rule.overrides && rule.overrides.length > 0) 
        {
            problem.overrides = rule.overrides;
        }

        return problem;
    }

    /**
     * Check if all of the conditions within a rule are met.  Called after the initial pattern finds an issue
     * 
     * @param {Condition[]} conditions the array of conditions for the rule that triggered
     * @param {string} documentContents the document we are currently looking through
     * @param {Range} findingRange the span of text for the current finding
     * @param {string} langID the language we are working in
     */
    public static MatchesConditions(conditions: Condition[], documentContents: string, findingRange: Range, langID: string): boolean 
    {
        if (conditions != undefined && conditions && conditions.length != 0)
        {
            for (let condition of conditions) 
            {   
                //i know this looks weird - there is an object called pattern, nested inside another object called
                //pattern. Sorry, that was poor naming convention
                if(condition.pattern != undefined && condition.pattern && condition.pattern.pattern != undefined &&
                    condition.pattern.pattern && condition.pattern.pattern.length > 0)
                {
                    if(!DevSkimWorker.MatchesConditionPattern(condition, documentContents, findingRange, langID))
                    {
                        return false;
                    }
                }
                else if(condition.lambda != undefined && condition.lambda && condition.lambda.lambda_code != undefined &&
                    condition.lambda.lambda_code && condition.lambda.lambda_code.length > 0)
                {
                    let lambdaWorker : DevskimLambdaEngine = new DevskimLambdaEngine(condition, documentContents, findingRange, langID);
                    return lambdaWorker.ExecuteLambda(); 
                }

            }
        }

        return true;
    }

    /**
     * Check to see if a RegEx powered condition is met or not
     * 
     * @param {Condition} condition the condition objects we are checking for
     * @param {string} documentContents the document we are finding the conditions in
     * @param {Range} findingRange the location of the finding we are looking for more conditions around
     * @param {string} langID the language we are working in
     */
    public static MatchesConditionPattern(condition: Condition, documentContents: string, findingRange: Range, langID: string): boolean
    {
        let regionRegex: RegExp = /finding-region\s*\((-*\d+),\s*(-*\d+)\s*\)/;
        let XRegExp = require('xregexp');


        if (condition.negateFinding == undefined)
        {
            condition.negateFinding = false;
        }

        let modifiers: string[] = (condition.pattern.modifiers != undefined && condition.pattern.modifiers.length > 0) ?
            condition.pattern.modifiers.concat(["g"]) : ["g"];

        let conditionRegex: RegExp = DevSkimWorker.MakeRegex(condition.pattern.type, condition.pattern.pattern, modifiers, true);

        let startPos: number = findingRange.start.line;
        let endPos: number = findingRange.end.line;

        //calculate where to look for the condition.  finding-only is just within the actual finding the original pattern flagged.
        //finding-region(#,#) specifies an area around the finding.  A 0 for # means the line of the finding, negative values mean 
        //that many lines prior to the finding, and positive values mean that many line later in the code
        if (condition.search_in == undefined || condition.search_in.length == 0) 
        {
            startPos = DocumentUtilities.GetDocumentPosition(documentContents, findingRange.start.line);
            endPos = DocumentUtilities.GetDocumentPosition(documentContents, findingRange.end.line + 1);
        }
        else if (condition.search_in == "finding-only") 
        {
            startPos = DocumentUtilities.GetDocumentPosition(documentContents, findingRange.start.line) + findingRange.start.character;
            endPos = DocumentUtilities.GetDocumentPosition(documentContents, findingRange.end.line) + findingRange.end.character;
        }
        else 
        {
            let regionMatch = XRegExp.exec(condition.search_in, regionRegex);
            if (regionMatch && regionMatch.length > 2) 
            {
                startPos = DocumentUtilities.GetDocumentPosition(documentContents, findingRange.start.line + +regionMatch[1]);
                endPos = DocumentUtilities.GetDocumentPosition(documentContents, findingRange.end.line + +regionMatch[2] + 1);
            }
        }
        let foundPattern = false;
        //go through all of the text looking for a match with the given pattern
        let match = XRegExp.exec(documentContents, conditionRegex, startPos);
        while (match) 
        {
            //if we are passed the point we should be looking
            if (match.index > endPos) 
            {
                if (condition.negateFinding == false) 
                {
                    return false;
                }
                else 
                {
                    break;
                }
            }


            //calculate what line we are on by grabbing the text before the match & counting the newlines in it
            let lineStart: number = DocumentUtilities.GetLineNumber(documentContents, match.index);
            let newlineIndex: number = (lineStart == 0) ? -1 : documentContents.substr(0, match.index).lastIndexOf("\n");

            //look for the suppression comment for that finding
            if (DocumentUtilities.MatchIsInScope(langID, documentContents.substr(0, match.index), newlineIndex, condition.pattern.scopes)) 
            {
                if (condition.negateFinding == true) 
                {
                    return false;
                }
                else 
                {
                    foundPattern = true;
                    break;
                }
            }
            startPos = match.index + match[0].length;
            match = XRegExp.exec(documentContents, conditionRegex, startPos);
        }
        if (condition.negateFinding == false && foundPattern == false) 
        {
            return false;
        }
        

        return true;
    }

 

    /**
     * Create an array of fixes from the rule and the vulnerable part of the file being scanned
     *
     * @private
     * @param {Rule} rule the rule that triggered the issue
     * @param {string} replacementSource the text that should be replaced by the fixit
     * @param {Range} range the range in the document that should be swapped out by the fixit
     * @returns {DevSkimAutoFixEdit[]}
     *
     * @memberOf DevSkimWorker
     */
    public static MakeFixes(rule: Rule, replacementSource: string, range: Range): DevSkimAutoFixEdit[] 
    {
        const fixes: DevSkimAutoFixEdit[] = [];
        //if there are any fixes, add them to the fix collection so they can be used in code fix commands
        if (rule.fix_its !== undefined && rule.fix_its.length > 0) 
        {
            //recordCodeAction below acts like a stack, putting the most recently added rule first.
            //Since the very first fix in the rule is usually the preferred one (when there are multiples)
            //we want it to be first in the fixes collection, so we go through in reverse order 
            for (let fixIndex = rule.fix_its.length - 1; fixIndex >= 0; fixIndex--) 
            {
                let fix: DevSkimAutoFixEdit = Object.create(null);
                let replacePattern = DevSkimWorker.MakeRegex(rule.fix_its[fixIndex].pattern.type,
                    rule.fix_its[fixIndex].pattern.pattern, rule.fix_its[fixIndex].pattern.modifiers, false);

                try
                {
                    fix.text = replacementSource.replace(replacePattern, rule.fix_its[fixIndex].replacement);
                    fix.fixName = "DevSkim: " + rule.fix_its[fixIndex].name;

                    fix.range = range;
                    fixes.push(fix);
                }
                catch (e) 
                {
                    //console.log(e);
                }
            }
        }
        return fixes;
    }

    /**
     * Removes any findings from the problems array corresponding to rules that were overridden by other rules
     * for example, both the Java specific MD5 rule and the generic MD5 rule will trigger on the same usage of MD5
     * in Java.  We should only report the Java specific finding, as it supersedes the generic rule
     *
     * @private
     * @param {DevSkimProblem[]} problems array of findings
     * @returns {DevSkimProblem[]} findings with any overridden findings removed
     */
    private processOverrides(problems: DevSkimProblem[]): DevSkimProblem[] 
    {
        let overrideRemoved = false;

        for (let problem of problems) 
        {
            //if this problem overrides other ones, THEN do the processing
            if (problem.overrides.length > 0) 
            {
                //one rule can override multiple other rules, so create a regex of all
                //of the overrides so we can search all at once - i.e. override1|override2|override3
                let regexString: string = problem.overrides[0];
                for (let x = 1; x < problem.overrides.length; x++) 
                {
                    regexString = regexString + "|" + problem.overrides[x];
                }

                //now search all of the existing findings for matches on both the regex, and the line of code
                //there is some assumption that both will be on the same line, and it *might* be possible that they
                //aren't BUT we can't blanket say remove all instances of the overridden finding, because it might flag
                //issues the rule that supersedes it does not
                let x = 0;
                while ( x < problems.length )
                {
                    let matches = problems[x].ruleId.match(regexString);
                    let range: Range = (problem.suppressedFindingRange != null) ? problem.suppressedFindingRange : problem.range;

                    if ((matches !== undefined && matches != null && matches.length > 0)
                        && problems[x].range.start.line == range.start.line &&
                        (problems[x].range.start.character <= range.end.character &&   /* Range overlap algorithm */ 
                         range.start.character <= problems[x].range.end.character))
                    {
                        problems.splice(x, 1);
                        overrideRemoved = true;
                    }
                    else
                    {
                        x++;
                    }
                }
                //clear the overrides so we don't process them on subsequent recursive calls to this
                //function
                problem.overrides = []

            }
        }
        // I hate recursion - it gives me perf concerns, but because we are modifying the 
        //array that we are iterating over we can't trust that we don't terminate earlier than
        //desired (because the length is going down while the iterator is going up), so run
        //until we don't modify anymore.  To make things from getting too ugly, we do clear a 
        //problem's overrides after we processed them, so we don't run it again in 
        //recursive calls
        if (overrideRemoved) 
        {
            return this.processOverrides(problems)
        }
        else 
        {
            return problems;
        }
    }

    /**
     * compares the languageID against all of the languages listed in the applies_to array to check
     * for a match.  If it matches, then the rule/pattern applies to the language being analyzed.
     *
     * Also checks to see if applies_to has the specific file name for the current file
     *
     * Absent any value in applies_to we assume it applies to everything so return true
     *
     * @param {string} languageID the vscode languageID for the current document
     * @param {string[]} applies_to the array of languages a rule/pattern applies to
     * @param {string} documentURI the current document URI
     * @returns {boolean} true if it applies, false if it doesn't
     */
    public static appliesToLangOrFile(languageID: string, applies_to: string[], documentURI: string): boolean
    {
        //if the parameters are empty, assume it applies.  Also, apply all the rules to plaintext documents	
        if (applies_to != undefined && applies_to && applies_to.length > 0) 
        {
            for (let applies of applies_to) 
            {
                //if the list of languages this rule applies to matches the current lang ID
                if (languageID !== undefined && languageID != null && languageID.toLowerCase() == applies.toLowerCase()) 
                {
                    return true;
                }
                else if (applies.indexOf(".") != -1 /*applies to is probably a specific file name instead of a langID*/
                    && documentURI.toLowerCase().indexOf(applies.toLowerCase()) != -1) /*and its in the current doc URI*/
                {
                    return true;
                }
            }
            return false;
        }
        else 
        {
            return true;
        }
    }

}