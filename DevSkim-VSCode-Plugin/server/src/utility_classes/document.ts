/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 *
 * Helper functions when working with the text of a document.  Methods are primarily static
 * as they don't require maintaining a state, and get invoked quite a lot.  While this increases
 * memory usage up front, it is more performant as it doesn't require continuos object creation/destruction
 * 
 * @export
 * @class DocumentUtilities
 */

import { SourceContext } from "./sourceContext";

export class DocumentUtilities
{
    public static newlinePattern: RegExp = /(\r\n|\n|\r)/gm;
    public static windowsOnlyNewlinePattern: RegExp = /(\r\n)/gm;
    

    /**
     * Returns the newline character conventions used in the document - \r\n if windows, \n otherwise
     * @param documentContents the document being parsed.  If \r\n is in the document, its assumed its using windows conventions
     */
    public static GetNewlineCharacter(documentContents: string) : string
    {
        let XRegExp = require('xregexp');

        //go through all of the text looking for a match with the given pattern
        let match = XRegExp.exec(documentContents, DocumentUtilities.windowsOnlyNewlinePattern, 0);
        if(match)
        {
            return "\r\n";
        }
        else
        {
            return "\n";
        }        

    }
    /**
     * The documentContents is just a stream of text, but when interacting with the editor its common to need
     * the line number.  This counts the newlines to the current document position
     *
     * @private
     * @param {string} documentContents the text to count newlines in
     * @param {number} currentPosition the point in the text that we should count newlines to
     * @returns {number}
     *
     */
    public static GetLineNumber(documentContents: string, currentPosition: number): number 
    {

        let subDocument: string = documentContents.substr(0, currentPosition);
        let linebreaks: RegExpMatchArray = subDocument.match(DocumentUtilities.newlinePattern);
        return (linebreaks !== undefined && linebreaks !== null) ? linebreaks.length : 0;
    }

    /**
     * Given the line number, find the number of characters in the document to get to that line number
     * @param {string} documentContents the document we are parsing for the line
     * @param {number} lineNumber the VS Code line number (internally, not UI - internally lines are 0 indexed, in the UI they start at 1)
     */
    public static GetDocumentPosition(documentContents: string, lineNumber: number): number 
    {
        if (lineNumber < 1)
            return 0;
        //the line number is 0 indexed, but we are counting newlines, which isn't, so add 1
        lineNumber++;

        let line = 1;
        let matchPosition = 0;
        let XRegExp = require('xregexp');

        //go through all of the text looking for a match with the given pattern
        let match = XRegExp.exec(documentContents, DocumentUtilities.newlinePattern, matchPosition);
        while (match) 
        {
            line++;
            matchPosition = match.index + match[0].length;

            if (line == lineNumber)
                return matchPosition;

            match = XRegExp.exec(documentContents, DocumentUtilities.newlinePattern, matchPosition);
        }

        return documentContents.length;
    }

    /**
     * Retrieves the full line of text for the given line number
     * @param documentContents document string that the line is being extracted from
     * @param lineNumber line number to extract 
     */
    public static GetLine(documentContents : string, lineNumber: number) : string
    {
        if(lineNumber < 0 )
            return documentContents;

        let startPosition : number = DocumentUtilities.GetDocumentPosition(documentContents, lineNumber);
        return DocumentUtilities.GetPartialLine(documentContents, startPosition);
             

    }

    /**
     * Retrieves the full line of text that the given documentPosition is in
     * @param documentContents document string that the line is being extracted from
     * @param documentPosition the character position within the documentContents, whose line contents will be retrieved
     */
    public static GetLineFromPosition(documentContents : string, documentPosition: number) : string
    {
        if(documentPosition < 0 )
            return documentContents;
            
        let lineNumber : number = DocumentUtilities.GetLineNumber(documentContents,documentPosition);
        return DocumentUtilities.GetLine(documentContents, lineNumber);
    }

    /**
     * retrieves the partial line of text, starting at the specified document position
     * @param documentContents document string that the line is being extracted from
     * @param documentPosition the starting point within the line that will mark the beginning of the returned text
     */
    public static GetPartialLine(documentContents: string, documentPosition: number) : string
    {
        let XRegExp = require('xregexp');

        let match = XRegExp.exec(documentContents, DocumentUtilities.newlinePattern, documentPosition);
        return (match) ? documentContents.substr(documentPosition, match.index - documentPosition) : documentContents.substr(documentPosition);           
    }

    /**
     * get the number of white spaces at the beginning of the line - used to ensure formatting with an inserted line
     * @param documentContents the document being analyzed
     * @param lineNumber the current line number to inspect for leading whitespace
     * @return a string duplicating the whitespace at the beginning of the line 
     */
    public static GetLeadingWhiteSpace(documentContents : string, lineNumber: number) : string
    {
        let leadingWhitespacePattern:  RegExp = /^([ \t]+)/gm;
        let lineText : string = DocumentUtilities.GetLine(documentContents, lineNumber);
        let XRegExp = require('xregexp');

        let match = XRegExp.exec(lineText, leadingWhitespacePattern);


        return (match) ? match[1] : ""; 

    }

    /**
     * Check to see if the finding occurs within the scope expected
     * see scope param for details
     *
     * @public
     * @param {string} langID the VS code language identifier for the document being analyzed
     * @param {string} docContentsToFinding the contents up to, but not including the finding
     * @param {number} newlineIndex numeric index of the newline
     * @param {string} scopes values are code (finding should only occur in code), comment (finding should only occur code comments), or all (finding occurs anywhere)
     * @returns {boolean}
     * @memberof DevSkimWorker
     */
    public static MatchIsInScope(langID: string, docContentsToFinding: string, newlineIndex: number, scopes: string[]): boolean 
    {
        if (scopes.indexOf("all") > -1)
            return true;

        let findingInComment: boolean = SourceContext.IsFindingInComment(langID, docContentsToFinding, newlineIndex);

        for (let scope of scopes) 
        {
            if ((scope == "code" && !findingInComment) || (scope == "comment" && findingInComment))
                return true;
        }
        return false;
    }    
}