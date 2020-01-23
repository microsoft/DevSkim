/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 *
 * Class for executing lambdas from conditionals, as well as all of the support functions
 * and values that can be used within the lambda
 * 
 * since the lambda expects an arrow function, () => {}, all of the code within the arrow function
 * has access to "this", which means it can call any of the functions or access any of the properties
 * of this class
 * 
 * @export
 * @class LambdaEngine
 */

import { Range } from 'vscode-languageserver';
import { Condition } from "./devskimObjects";

export class DevskimLambdaEngine
{
    private lambdaCode : string;
    private condition : Condition;
    private documentContents : string;
    private findingRange : Range;
    private langID : string;

    /**
     * Build the initial lambda object for execution
     * @param currentCondition the condition object containing the lambda
     * @param currentDocumentContents the document that's currently being analyzed
     * @param currentFindingRange the location of the finding that triggered this flow
     * @param currentLangID the VS Code language identifier for the language of the document
     */
    constructor(currentCondition: Condition, currentDocumentContents: string, currentFindingRange: Range, currentLangID: string)
    {
        this.lambdaCode = currentCondition.lambda.lambda_code;
        this.condition = currentCondition;
        this.documentContents = currentDocumentContents;
        this.findingRange = currentFindingRange;
        this.langID = currentLangID;        
    }

    /**
     * Run the lambda from the condition
     * @return true if the condition the lambda searches for was found, false otherwise (assuming the lambda author constructed the lambda correctly)
     */
    public ExecuteLambda() : boolean
    {
        //There is a known risk here.  This code is coming from a rule, so if that rule is editable by a malicious party
        //the can inject code.  That said, within the github repo (or the VS marketplace) they could edit the source directly
        //and bypass the rule, and on a user machine if they can edit the rule on the file system they likely have a multitude
        //of simpler avenues for code execution, so the risk doesn't seem noteworthy.  If someone can think of an avenue where
        //its possible to edit the json on the user machine without otherwise having code execution capability, please let us
        //know 
        let lambdaFunction = eval(this.lambdaCode);
        return lambdaFunction();
    }
}