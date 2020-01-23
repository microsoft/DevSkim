/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 *
 * Class to load the json based rules from the file system
 * 
 * @export
 * @class RulesLoader
 */
const util = require('util');
const fs = require('fs');
const readFile = util.promisify(fs.readFile);
const readdir = require('recursive-readdir');
import { Rule } from '../devskimObjects';
import { RuleValidator } from './ruleValidator';
import { DebugLogger } from './logger';



export interface IRulesLoader
{
    loadRules(rulesDirectory?: string): Promise<Rule[]>;
    validateRules(rules: Rule[]);
}

export class RulesLoader
{
    private analysisRules: Rule[] = [];

    constructor(private logger: DebugLogger, private validate: boolean = true, private rulesDirectory?: string)
    {
    }

    public async loadRules(rulesDirectory?: string): Promise<Rule[]>
    {

        const tempRules: Rule[] = [];
        const rootDir = rulesDirectory ? rulesDirectory : this.rulesDirectory;
        this.logger.log(`RulesLoader: loadRules() starting ...`);
        this.logger.log(`RulesLoader: loadRules() from ${rootDir}`);

        const filesFound = await readdir(rootDir, ["!*.json"])
            .then(files => (files))
            .catch(e =>
            {
                this.logger.log(`loadRules exception: ${e.message}`);
            });
        for (let filePath of filesFound)
        {
            await readFile(filePath).then(content =>
            {
                const loadedRules: Rule[] = JSON.parse(content);
                if (loadedRules)
                {
                    for (let rule of loadedRules)
                    {
                        if (!rule.name)
                        {
                            continue;
                        }
                        rule.filepath = filePath;
                        tempRules.push(rule);
                    }
                    this.logger.log(`DevSkimWorker loadRules() so far: ${tempRules.length || 0}.`);
                }
            });
        }
        return tempRules;
    }

    public async validateRules(rules: Rule[]): Promise<Rule[]>
    {
        let validator: RuleValidator = new RuleValidator(this.logger, this.rulesDirectory, this.rulesDirectory);
        this.analysisRules =
            await validator.validateRules(rules, this.validate);
        this.logger.log(`RulesLoader: validateRules() done. Rules found: ${this.analysisRules.length || 0}.`);
        return this.analysisRules;
    }
}