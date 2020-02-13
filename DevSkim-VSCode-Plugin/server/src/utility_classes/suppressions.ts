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
import { DevSkimAutoFixEdit, DevskimRuleSeverity, IDevSkimSettings } from "../devskimObjects";
import { Range } from 'vscode-languageserver';
import { SourceContext } from "./sourceContext";
import { DocumentUtilities } from './document';

/**
 * Class to handle Suppressions (i.e. comments that direct devskim to ignore a finding for either a period of time or permanently)
 * a suppression in practice would look something like this (assuming a finding in a C file):
 * 
 *      strcpy(a,b); //DevSkim: ignore DS185832 until 2016-12-28
 * 
 * The comment after strcpy (which DevSkim would normally flag) tells devskim to ignore that specific finding (as Identified by the DS185832 - the strcpy rule)
 * until 2016-12-28.  prior to that date DevSkim shouldn't flag the finding. After that date it should.  This is an example of a temporary suppression, and is used
 * when the dev wants to fix something but is busy with other work at the moment.  If the date is omitted DevSkim will never flag that finding again (provided the 
 * suppression comment remains next to the finding).
 * 
 * The logic to determine if a finding should be suppressed, as well as the logic to create the code action to add a suppression exist in this class
 * @export
 * @class DevSkimSuppression
 */
export class DevSkimSuppression
{
    public static suppressionRegEx: RegExp = /DevSkim: ignore ([^\s]+)(?:\suntil ((\d{4})-(\d{2})-(\d{2})))?/i;
    public static reviewRegEx: RegExp = /DevSkim: reviewed ([^\s]+)(?:\son ((\d{4})-(\d{2})-(\d{2})))?/i;

    /**
     * Instantiate a Suppressions object.  This is necessary to insert a new suppression, but if 
     * the only action necessary is checking if a finding is already suppressed, the static method
     * isFindingCommented() should be used without instantiating the class
     * @param dsSettings - the current settings, necessary to determine preferred comment style and reviewer name
     */
    constructor(public dsSettings: IDevSkimSettings)
    {

    }

    /**
    * Create an array of Code Action(s) for the user to invoke should they want to suppress or mark a finding reviews
    * 
    * @param {string} ruleID the rule to be suppressed
    * @param {string} documentContents the current document
    * @param {number} startCharacter the start point of the finding
    * @param {number} lineStart the line the finding starts on
    * @param {string} langID the language for the file according to VSCode (so that we can get the correct comment syntax)
    * @param {DevskimRuleSeverity} ruleSeverity (option) the severity of the rule - necessary if the rule is a Manual Review rule, since slightly different
    *                                           logic is employed because of the different comment string.  If omitted, assume a normal suppression 
    * @returns {DevSkimAutoFixEdit[]} an array of code actions for suppressions (usually "Suppress X Days" and "Suppress Indefinitely")
    * 
    * @memberOf DevSkimSuppression
    */
    public createActions(ruleID: string, documentContents: string, startCharacter: number, lineStart: number,
        langID: string, ruleSeverity: DevskimRuleSeverity): DevSkimAutoFixEdit[]
    {
        let codeActions: DevSkimAutoFixEdit[] = [];
        let isReviewRule = (ruleSeverity !== undefined
            && ruleSeverity != null
            && ruleSeverity == DevskimRuleSeverity.ManualReview);

        //if this is a suppression and temporary suppressions are enabled (i.e. the setting for suppression duration is > 0) then
        //first add a code action for a temporary suppression
        if (!isReviewRule && this.dsSettings.suppressionDurationInDays > 0)
        {
            codeActions.push(this.addAction(ruleID, documentContents, startCharacter, lineStart,
                langID, isReviewRule, this.dsSettings.suppressionDurationInDays));
        }

        //now either add a code action to mark this reviewed, or to suppress the finding indefinitely
        codeActions.push(this.addAction(ruleID, documentContents, startCharacter, lineStart, langID, isReviewRule));
        return codeActions;
    }

    /**
     * Create a Code Action for the user to invoke should they want to suppress a finding
     * 
     * @private
     * @param {string}  ruleID the rule to be suppressed
     * @param {string}  documentContents the current document
     * @param {number}  startCharacter the start point of the finding
     * @param {number}  lineStart the line the finding starts on
     * @param {string}  langID the language for the file according to VSCode (so that we can get the correct comment syntax)
     * @param {boolean} isReviewRule true if this is a manual review rule - the text is slightly different if it is
     * @param {number}  daysOffset the number of days in the future a time based suppression should insert, comes from the user settings
     * @returns {DevSkimAutoFixEdit[]} an array of code actions for suppressions (usually "Suppress X Days" and "Suppress Indefinitely")
     * 
     * @memberOf DevSkimSuppression
     */
    private addAction(ruleID: string, documentContents: string, startCharacter: number, lineStart: number,
        langID: string, isReviewRule: boolean, daysOffset: number = -1): DevSkimAutoFixEdit
    {
        let action: DevSkimAutoFixEdit = Object.create(null);
        let regex: RegExp = (isReviewRule)
            ? DevSkimSuppression.reviewRegEx
            : DevSkimSuppression.suppressionRegEx;
        let startingWhitespace = " ";

        this.setActionFixName(isReviewRule, action, ruleID, daysOffset);

        //make the day in the future that a time based expression will expire
        const date = new Date();
        if (!isReviewRule && daysOffset > 0)
        {
            date.setDate(date.getDate() + daysOffset);
        }

        //find the end of the current line of the finding
        let XRegExp = require('xregexp');
        let range: Range;
        let match;

        //start off generating a new suppression.  If its going on the same line as the finding look for the
        //newline (if it exists) and insert just before it
        if(this.dsSettings.suppressionCommentPlacement == "same line as finding")
        {
            //check to see if this is the end of the document or not, as there is no newline at the end
            match = XRegExp.exec(documentContents, DocumentUtilities.newlinePattern, startCharacter);
            if (match)
            {
                let columnStart = (lineStart == 0)
                    ? match.index
                    : match.index - documentContents.substr(0, match.index).lastIndexOf("\n") - 1;
                range = Range.create(lineStart, columnStart, lineStart, columnStart + match[0].length);
                documentContents = documentContents.substr(0, match.index);
            }
            else
            {
                //replace with end of file
                let columnStart = (lineStart == 0)
                    ? documentContents.length
                    : documentContents.length - documentContents.lastIndexOf("\n") - 1;
                range = Range.create(lineStart, columnStart, lineStart, columnStart);
            }
        }
        //if the suppression goes on the line above the logic is much simpler - we just insert at the front
        //of the line and below we add a newline at the end of the suppression
        else
        {
            range = Range.create(lineStart, 0, lineStart, 0);
            startingWhitespace = DocumentUtilities.GetLeadingWhiteSpace(documentContents, lineStart);

        }

        // if there is an existing suppression that has expired (or there for a different issue)
        // then it needs to be replaced
        let existingSuppression : DevSkimSuppressionFinding;
        let suppressionStart : number = startCharacter;
        let suppressionLine : number = lineStart;

        //this checks for any existing suppression, regardless of whether it is expired, or for a different finding, because regardless
        //that comment gets modified
        existingSuppression= DevSkimSuppression.isFindingCommented(startCharacter,documentContents,ruleID,langID, isReviewRule, true);
        //yep, there is an existing suppression, so start working off of its location
        if (existingSuppression.showSuppressionFinding)
        {
            suppressionStart = DocumentUtilities.GetDocumentPosition(documentContents, existingSuppression.suppressionRange.start.line);
            suppressionLine = existingSuppression.suppressionRange.start.line;
        }
        //now get the actual suppression text out so it can be modified
        match = XRegExp.exec(documentContents, regex, suppressionStart);
        if (match && DocumentUtilities.GetLineNumber(documentContents, match.index) == suppressionLine)
        {
            //parse the existing suppression and set the range/text to modify it
            let columnStart: number = (suppressionLine == 0) ? match.index : match.index - documentContents.substr(0, match.index).lastIndexOf("\n") - 1;
            range = Range.create(suppressionLine, columnStart, suppressionLine, columnStart + match[0].length);
            if (match[1] !== undefined && match[1] != null && match[1].length > 0)
            {
                //the existing ruleID was found, so just set it to that string (may include other rules too)
                //this would be an instance where the date has expired
                if (match[1].indexOf(ruleID) >= 0)
                {
                    ruleID = match[1];
                }
                //the finding rule id wasn't found, so this is an instance where there is a separate finding suppressed
                //on the same line.  Append
                else
                {
                    ruleID = ruleID + "," + match[1];
                }
            }
            if (isReviewRule || daysOffset > 0)
            {
                action.text = this.makeActionString(ruleID, isReviewRule, date);
            }
            else
            {
                action.text = this.makeActionString(ruleID, isReviewRule);
            }
        }

        // if there is not an existing suppression we need to create the full suppression text
        else
        {
            let StartComment: string = "";
            let EndComment : string = "";

            //select the right comment type, based on the user settings and the
            //comment capability of the programming language
            if(this.dsSettings.suppressionCommentStyle == "block")
            {
                StartComment = SourceContext.GetBlockCommentStart(langID);
                EndComment = SourceContext.GetBlockCommentEnd(langID);
                if (!StartComment || StartComment.length < 1 || !EndComment || EndComment.length < 1)
                {
                    StartComment = SourceContext.GetLineComment(langID);
                }                   
            }
            else
            {
                StartComment = SourceContext.GetLineComment(langID);
                if (!StartComment || StartComment.length < 1)
                {
                    StartComment = SourceContext.GetBlockCommentStart(langID);
                    EndComment = SourceContext.GetBlockCommentEnd(langID);
                }                
            }
            
            
            let optionalNewline: string = "";
            
            //we will need a newline if this suppression is supposed to go above the finding
            if (this.dsSettings.suppressionCommentPlacement == "line above finding") 
            {
                optionalNewline = DocumentUtilities.GetNewlineCharacter(documentContents);
            }
            
            //make the actual text inserted as the suppression            
            if (isReviewRule || daysOffset > 0)
            {
                action.text = startingWhitespace + StartComment + this.makeActionString(ruleID, isReviewRule, date) + " " + EndComment + optionalNewline;
            }
            else
            {
                action.text = startingWhitespace + StartComment + this.makeActionString(ruleID, isReviewRule) + " " + EndComment + optionalNewline;
            }
        }
        action.range = range;
        return action;
    }

    /**
     * Create the string that goes into the IDE UI to allow a user to automatically create a suppression
     * @param isReviewRule True if this is a manual review rule - the text is slightly different if it is
     * @param action the associated action that is triggered when the user clicks on this name in the IDE UI
     * @param ruleID the rule id for the current finding
     * @param daysOffset how many days to suppress the finding, if this 
     */
    private setActionFixName(isReviewRule: boolean, action: DevSkimAutoFixEdit, ruleID: string, daysOffset: number = -1)
    {
        // These are the strings that appear on the light bulb menu to the user.
        // @todo: make localized.  Right now these are the only hard coded strings in the app.
        //  The rest come from the rules files and we have plans to make those localized as well
        if (isReviewRule)
        {
            action.fixName = `DevSkim: Mark ${ruleID} as Reviewed`;
        } 
        else if (daysOffset > 0)
        {
            action.fixName = `DevSkim: Suppress ${ruleID} for ${daysOffset.toString(10)} days`;
        } 
        else
        {
            action.fixName = `DevSkim: Suppress ${ruleID} permanently`;
        }
    }

    /**
     * Determine if there is a suppression comment in the line of the finding, if it
     * corresponds to the rule that triggered the finding, and if there is a date the suppression
     * expires.  Return true if the finding should be suppressed for now, so that it isn't added
     * to the list of diagnostics
     * 
     * @private
     * @param {number} startPosition the start of the finding in the document (#of chars from the start)
     * @param {string} documentContents the content containing the finding
     * @param {string} ruleID the rule that triggered the finding
     * @param {string} langID the VS Code language ID for the file being analyzed (to look up comment syntax)
     * @param {boolean} isReviewRule true if this is a manual review rule, otherwise false, as the comment syntax is different for review rules
     * @param {boolean} anySuppression will look for any suppression, regardless of if it doesn't match ruleID, or is expired
     * @returns {boolean} true if this finding should be ignored, false if it shouldn't
     * 
     * @memberOf DevSkimWorker
     */
    public static isFindingCommented(startPosition: number, documentContents: string, ruleID: string, langID : string,
        isReviewRule: boolean, anySuppression : boolean = false): DevSkimSuppressionFinding
    {
        let XRegExp = require('xregexp');
        let regex: RegExp = (isReviewRule) ? DevSkimSuppression.reviewRegEx : DevSkimSuppression.suppressionRegEx;
        let line : string;
        let returnFinding : DevSkimSuppressionFinding;

        //its a little ugly to have a local function, but this is a convenient way of recalling this code repeatedly
        //while not exposing it to any other function.  This code checks to see if a suppression for the current issue
        //is present in the line of code being analyzed. It also allows the use of the current Document without increasing
        //its memory footprint, given that this code has access to the parent function scope as well
        /**
         * Check if the current finding is suppressed on the line of code provided
         * @param line the line of code to inspect for a suppression
         * @param startPosition where in the document the line starts (for calculating line number)
         * 
         * @returns {DevSkimSuppressionFinding} a DevSkimSuppressionFinding - this object is used to highlight the DS#### in the suppression
         *                                      so that mousing over it provides details on what was suppressed
         */
        let suppressionCheck = (line: string, startPosition: number) : DevSkimSuppressionFinding =>
        {
            let finding: DevSkimSuppressionFinding = Object.create(null);
            finding.showSuppressionFinding = false;
            
            //look for the suppression comment
            let match = XRegExp.exec(line, regex);
            if (match)
            {
                let suppressionIndex : number = match[0].indexOf(ruleID);
                if (suppressionIndex > -1 || anySuppression)
                {
                    let lineStart : number = DocumentUtilities.GetLineNumber(documentContents,startPosition);
                    suppressionIndex += match.index;
                    finding.suppressionRange = Range.create(lineStart, suppressionIndex, lineStart, suppressionIndex + ruleID.length);
                    finding.noRange = false;
                    if (!isReviewRule && match[2] !== undefined && match[2] != null && match[2].length > 0)
                    {
                        const untilDate: number = Date.UTC(match[3], match[4] - 1, match[5], 0, 0, 0, 0);
                        //we have a match of the rule, and haven't yet reached the "until" date, so ignore finding
                        //if the "until" date is less than the current time, the suppression has expired and we should not ignore
                        if (untilDate > Date.now() || anySuppression) 
                        {
                            finding.showSuppressionFinding = true;
                        }
                    }
                    else //we have a match with the rule (or all rules), and no "until" date, so we should ignore this finding
                    {
                        finding.showSuppressionFinding = true;
                    }
                }
                else if (match[0].indexOf("all") > -1)
                {
                    finding.showSuppressionFinding = true;
                    finding.noRange = true;
                }
            }
            return finding;
        };        
        
        
        let lineNumber : number = DocumentUtilities.GetLineNumber(documentContents,startPosition);
        startPosition = DocumentUtilities.GetDocumentPosition(documentContents, lineNumber);
        
        line = DocumentUtilities.GetPartialLine(documentContents, startPosition);
        returnFinding = suppressionCheck(line, startPosition); 
        
        //we didn't find a suppression on the same line, but it might be a comment on the previous line
        if(!returnFinding.showSuppressionFinding)
        { 
            lineNumber--;           
            while(lineNumber > -1)            
            {                
                //get the start position of the current line we are looking for a comment in           
                startPosition = DocumentUtilities.GetDocumentPosition(documentContents, lineNumber);
                
                //extract the line, and trim off the trailing space
               // let match = XRegExp.exec(documentContents, DocumentUtilities.newlinePattern, startPosition);
                //let secondLastMatch = (lineNumber -1 > -1) ? XRegExp.exec(documentContents, DocumentUtilities.newlinePattern, DocumentUtilities.GetDocumentPosition(documentContents, lineNumber -1)) : false;
                //let lastMatch = (secondLastMatch) ? secondLastMatch.index : startPosition;
                //let subDoc : string = documentContents.substr(0, (match) ? match.index : startPosition);
                let subDoc : string = DocumentUtilities.GetLineFromPosition(documentContents, startPosition);

                //check if the last line is a full line comment
                if(SourceContext.IsLineCommented(langID, subDoc))
                {                    
                    returnFinding = suppressionCheck(subDoc, startPosition); 
                    if(returnFinding.showSuppressionFinding)
                    {
                        break;
                    }
                }
                //check if its part of a block comment
                else if(SourceContext.IsLineBlockCommented(langID, documentContents, lineNumber))
                {
                    let commentStart : number = SourceContext.GetStartOfLastBlockComment(langID,documentContents.substr(0,startPosition + subDoc.length));
                    let doc : string = DocumentUtilities.GetLineFromPosition(documentContents, commentStart).trim();

                    if(SourceContext.GetStartOfLastBlockComment(langID,doc) == 0)
                    {
                        returnFinding = suppressionCheck(subDoc, commentStart); 
                        if(returnFinding.showSuppressionFinding)
                        {
                            break;
                        }
                    }
                }                
                else
                {
                    break;
                }
                lineNumber--;  
            }
        }

        return returnFinding;
    }

    /**
     * Generate the string that gets inserted into a comment for a suppression
     *
     * @private
     * @param {string} ruleIDs the DevSkim Rule ID that is being suppressed or reviewed (e.g. DS102158). Can be a list of IDs, comma separated (eg. DS102158,DS162445) if suppressing
     *                         multiple issues on a single line
     * @param {boolean} isReviewRule different strings are used if this is a code review rule versus a normal rule; one is marked reviewed and the other suppressed
     * @param {Date} date (optional) if this is a manual review rule (i.e. rule someone has to look at) this should be today's date, signifying that the person has reviewed the finding today.
     *                    if it is a suppression (i.e. a normal finding) this is the date that they would like to be reminded of the finding.  For example, if someone suppresses a finding for
     *                    thirty days this should be today + 30 days.  If omitted for a suppression the finding will be suppressed permanently
     * @returns {string}
     *
     * @memberOf DevSkimSuppression
     */
    private makeActionString(ruleIDs: string, isReviewRule: boolean, date?: Date): string
    {
        let actionString: string = (isReviewRule) ? "DevSkim: reviewed " : "DevSkim: ignore ";

        actionString += ruleIDs;
        if (date !== undefined && date != null && (date.getTime() > Date.now() || isReviewRule))
        {
            //both month and day should be in two digit format, so prepend a "0".  Also, month is 0 indexed so needs to be incremented
            //to be in a format that reflects how months are actually represented by humans (and every other programming language)
            const day: string = (date.getDate() > 9) ? date.getDate().toString() : "0" + date.getDate().toString();
            const month: string = ((date.getMonth() + 1) > 9) ? (date.getMonth() + 1).toString(10) : "0" + (date.getMonth() + 1).toString(10);

            actionString = (isReviewRule) ? actionString + " on " : actionString + " until ";
            actionString = actionString + date.getFullYear() + "-" + month + "-" + day;
        }
        if (isReviewRule && this.dsSettings.manualReviewerName !== undefined
            && this.dsSettings.manualReviewerName != null
            && this.dsSettings.manualReviewerName.length > 0)
        {
            actionString = `${actionString} by ${this.dsSettings.manualReviewerName}`;
        }
        return actionString;
    }
}

export class DevSkimSuppressionFinding
{
    /**
     * True to display the suppression finding 
     * e.g. put an informational squiggle with the finding text on the DS##### 
     * in the suppression
     */
    public showSuppressionFinding: boolean;

    /** 
     * The location of DS###### in the suppression
     */
    public suppressionRange: Range;

    /**
     * true if suppressionRange wasn't set or should be ignored but showSuppressionFinding is true
     * used because verifying that all of the range values were appropriately set is a pain
     */
    public noRange : boolean;
}
