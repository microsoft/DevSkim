/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * contains the logic to validate each rule in a file to make sure it matches the expected format 
 * 
 */

import {Rule} from "../devskimObjects";
import {IConnection} from "vscode-languageserver";
import {IRuleValidator} from "../utility_classes/ruleValidator";


/**
 * 
 */
export class RuleValidator implements IRuleValidator
{
    /**
     *
     * @param connection
     * @param rd
     * @param ed
     */
    constructor(private connection: IConnection, rd: string, ed: string)
    {
    }

    /**
     *
     * @param readRules
     * @param outputValidation
     */
    public validateRules(readRules : Rule[], outputValidation : boolean): Promise<Rule[]>
    {
        let r: Rule[] = [];
        return undefined 
    }
}
