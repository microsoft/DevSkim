/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ 
 * Contains the logic to validate each rule in a file to make sure it matches the expected format
 *
 */

import { Rule, FixIt, Pattern, Condition, Lambda } from "../devskimObjects";
import * as path from 'path';
import { DebugLogger } from "./logger";
import ErrnoException = NodeJS.ErrnoException;
import { noop } from "@babel/types";
const mkdirp = require('mkdirp');

export interface IRuleValidator
{
    validateRules(readRules: Rule[], outputValidation: boolean): Promise<Rule[]>;
}

/**
 * Class to validate each of the rules to ensure that they are in the expected format
 * or output warning messages if they are not
 */
export class RuleValidator implements IRuleValidator
{
    private rulesDir: string;
    private readonly errorDir: string;
    private readonly fixedRules: 
    {
        [fileName: string]: Rule[];
    };
    private outputMessages: OutputMessages[];
    private fs = require('fs');
    private writeoutNewRules: boolean;
    private logFilePath: string = '';

    /**
     * Build the validator object
     * @param logger the object used to log any errors with the actual processing (not errors in rules, but with the parser)
     * @param rulesDirectory the directory the rules are contained in
     * @param errorOutputDirectory where to write out an error file
     */
    constructor(private logger: DebugLogger, rulesDirectory: string, errorOutputDirectory: string)
    {
        this.rulesDir = rulesDirectory;
        this.errorDir = errorOutputDirectory;
        this.fixedRules = {};
        this.writeoutNewRules = false;
    }

    /**
     * Go through all of the loaded rules, and call logic to validate each field
     * @param readRules collection of all of the loaded rules
     * @param outputValidation if true, a log of mistakes in rules will be written out, as well as fixed rules when a fix is possible
     */
    public async validateRules(readRules: Rule[], outputValidation: boolean): Promise<Rule[]>
    {
        let rules: Rule[] = [];
        this.outputMessages = [];
        this.writeoutNewRules = false;

        if (!readRules) readRules = [];

        //We intentionally throw exceptions when a rule catastrophically fails validation , but that will get logged in the output message
        //so we don't actually need to handle the exception.  The catch block is empty because we want to keep
        //processing the rest of the rules
        for (let loadedRule of readRules)
        {
            try 
            {
                let newRule = (outputValidation) ? this.makeRule(loadedRule) : this.makeRuleNoValidation(loadedRule);
                rules.push(newRule);
            }
            catch (err)
            {                        
                noop();       
            }
        }

        //if told to outputValidation, write out fixed rules (if any) and the output log
        if (outputValidation)
        {
            if (this.outputMessages.length > 0)
            {
                this.logFilePath = path.join(this.errorDir, "rulesValidationLog.json1");
                await this.fs.writeFile(this.logFilePath, JSON.stringify(this.outputMessages, null, 4),
                    (err: ErrnoException) =>
                    {
                        if (err)
                        {
                            this.logger.log(`RuleValidator - outputValidation  err: ${err.message}`);
                        }
                    });
            }
            if (this.writeoutNewRules)
            {
                let newrulePath = path.join(this.errorDir, "..", "newrules");
                this.logger.log(`RuleValidator - outputValidation: ${newrulePath}`);

                for (let key in this.fixedRules)
                {
                    let filePath: string = key.substr(key.indexOf("rules") + 5);
                    filePath = path.join(newrulePath, filePath);
                    try
                    {
                        mkdirp.sync(path.dirname(filePath));
                        this.logger.log(`RuleValidator - newRulePath - file: ${filePath}`);
                        await this.fs.writeFileSync(filePath, JSON.stringify(this.fixedRules[key], null, 4));
                    }
                    catch (err)
                    {
                        this.logger.log(`RuleValidator - validateRules err: >>${err.message}<<`);
                    }
                }
            }
        }
        return rules;
    }

    /**
     * Loads a rule with fairly little validation (some default values, like scope, are filled in if missing)
     * @param loadedRule  rule loaded from File System whose severity is being validated 
     */
    private makeRuleNoValidation(loadedRule): Rule
    {
        let newRule: Rule = Object.create(null);

        newRule.name = loadedRule.name;
        newRule.id = loadedRule.id;
        newRule.description = loadedRule.description;
        newRule.recommendation = loadedRule.recommendation;

        if (RuleValidator.isSet(loadedRule.overrides, "array") && loadedRule.overrides.length > 0)
            newRule.overrides = loadedRule.overrides;

        if (RuleValidator.isSet(loadedRule.applies_to, "array") && loadedRule.applies_to.length > 0)
            newRule.applies_to = loadedRule.applies_to;

        if (RuleValidator.isSet(loadedRule.tags, "array") && loadedRule.tags.length > 0)
            newRule.tags = loadedRule.tags;

        newRule.severity = loadedRule.severity.toLowerCase();
        newRule._comment = (RuleValidator.isSet(loadedRule._comment, "string")) ? loadedRule._comment : "";

        newRule.ruleInfo = loadedRule.rule_info;

        newRule.patterns = loadedRule.patterns;
        for (let x = 0; x < newRule.patterns.length; x++)
        {
            //despite *mostly* not doing validation in this function, we do validate scope, as it may be missing and need a default value
            newRule.patterns[x].scopes = this.validatePatternScopeArray(newRule.patterns[x].scopes, loadedRule);
        }

        if (RuleValidator.isSet(loadedRule.fix_its, "array") && loadedRule.fix_its.length > 0)
            newRule.fix_its = loadedRule.fix_its;

        if (RuleValidator.isSet(loadedRule.conditions, "array") && loadedRule.conditions.length > 0)
            newRule.conditions = loadedRule.conditions;

        return newRule;
    }

    /**
     * Go through the object, validating all of the various properties to ensure they are present (if required) and in the
     * expected format
     * @param loadedRule rule loaded from File System whose severity is being validated
     */
    public makeRule(loadedRule: Rule): Rule
    {
        let newRule: Rule = Object.create(null);

        newRule.name = this.validateName(loadedRule);
        newRule.id = this.validateID(loadedRule);
        newRule.description = this.validateDescription(loadedRule);
        newRule.recommendation = this.validateRecommendation(loadedRule);

        let overrides: string[] = this.validateStringArray(loadedRule.overrides, "overrides", loadedRule, this.validateSpecificOverride);
        if (overrides.length > 0)
            newRule.overrides = overrides;

        let applies: string[] = this.validateStringArray(loadedRule.applies_to, "applies_to", loadedRule, RuleValidator.validateSpecificapplies_to);
        if (applies.length > 0)
            newRule.applies_to = applies;

        let tags: string[] = this.validateStringArray(loadedRule.tags, "tags", loadedRule, RuleValidator.validateSpecificTags);
        if (tags.length > 0)
            newRule.tags = tags;

        newRule.severity = this.validateSeverity(loadedRule);
        newRule._comment = (RuleValidator.isSet(loadedRule._comment, "string")) ? loadedRule._comment : "";

        newRule.ruleInfo = this.validateRuleInfo(loadedRule);

        newRule.patterns = this.validatePatternsArray(loadedRule);

        let fix_its: FixIt[] = this.validateFixitArray(loadedRule);
        if (fix_its.length > 0)
            newRule.fix_its = fix_its;

        newRule.conditions = this.validateConditionsArray(loadedRule);


        if (!RuleValidator.isSet(this.fixedRules[loadedRule.filepath], "array"))
        {
            this.fixedRules[loadedRule.filepath] = [];
        }
        this.fixedRules[loadedRule.filepath].push(newRule);
        return newRule;
    }

    /**
     * Loop through the array of conditions and validate each one
     * @param loadedRule the rule loaded from the file system
     */
    private validateConditionsArray(loadedRule) : Condition[]
    {
        let conditions: Condition[] = [];
        if (this.checkValue(loadedRule.conditions, loadedRule, "array", "", OutputAlert.Info))
        {
            for (let condition of loadedRule.conditions)
            {
                conditions.push(this.validateConditionObject(condition, loadedRule));
            }
        }
        return loadedRule.conditions;

    }

    /**
     * Ensure that the condition object was constructed appropriately, cleaning up deprecated/changed fields from previous
     * iterations of the object schema
     * @param loadedCondition the condition to inspect
     * @param loadedRule the rule object that the condition corresponds to
     */
    private validateConditionObject(loadedCondition, loadedRule) : Condition
    {
        let condition: Condition = Object.create(null);

        if(RuleValidator.isSet(loadedCondition.pattern, "Pattern"))
        {
            condition.pattern = this.validatePatternObject(loadedCondition.pattern, loadedRule);
        }
        else if(RuleValidator.isSet(loadedCondition.lambda, "Lambda"))
        {
            condition.lambda = this.validateLambda(loadedCondition.lambda, loadedCondition);
        }
        else
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Error;
            outcome.message = "Condition must have either a pattern or a lambda; neither was found";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;
            this.outputMessages.push(outcome);
            throw "Condition needs either a pattern or a lambda, and neither are present";
        }

        condition.negateFinding =  (RuleValidator.isSet(loadedCondition.negateFinding, "boolean")) ?
                                        loadedCondition.negateFinding : false ;        
        
        condition.search_in = this.validateSearch(loadedCondition.search_in, loadedRule);

        condition._comment = (RuleValidator.isSet(loadedCondition._comment, "string")) ? loadedCondition._comment : "";

        return condition;
    }

    /**
     * Inspect the lambda code to ensure it conforms to expectations
     * @param loadedLambda the lambda from the current condition within the current rule
     * @param loadedRule the current rule we are validating 
     * @todo right now there isn't much to do here, as lambda support is very early, but as it advances
     * this needs to be improved
     */
    private validateLambda(loadedLambda, loadedRule) : Lambda
    {
        let lambda : Lambda = Object.create(null);

        //To Do - actual validation.  Not sure what that will look like yet for lambda code
        //should at least verify the function signature
        lambda.lambda_code = loadedLambda.lambda_code;

        lambda._comment = (RuleValidator.isSet(loadedLambda._comment, "string")) ? loadedLambda._comment : "";

        return lambda;
    }

    /**
     * Ensure that the search_in value is either finding_only, absent, or finding_region(# ,#)
     * 
     * @param loadedSearch the search_in value within a loadedrule
     * @param loadedRule the current rule we are validating 
     */
    private validateSearch(loadedSearch, loadedRule) : string
    {
        let search_in : string = "";

        if(!RuleValidator.isSet(loadedSearch, "string"))
        {
            search_in = "finding-region(0,0)";
        }
        else if(loadedSearch == "finding-only")
        {
            search_in = loadedSearch;
        }
        else //its either finding_region(#,#) or garbage
        {
            let regionRegex: RegExp = /finding-region\s*\((-*\d+),\s*(-*\d+)\s*\)/;
            let XRegExp = require('xregexp');

            let regionMatch = XRegExp.exec(loadedSearch, regionRegex);
            if (regionMatch && regionMatch.length > 2) 
            {
                //make sure both of the parameters are actually numbers
                let startPos : number = this.verifyType(regionMatch[1], "number", loadedRule, OutputAlert.Warning, 
                                        "Condition has an invalid first parameter in search-in; defaulting to 0") ?
                                        regionMatch[1] : 0 ;

                let endPos : number = (this.verifyType(regionMatch[2], "number", loadedRule, OutputAlert.Warning, 
                                        "Condition has an invalid second parameter in search-in; defaulting to 0)")) ?
                                        regionMatch[2] : 0;
                
                search_in = "finding-region(" + startPos + "," + endPos + ")";
                
            }
            else  
            {     
                //if there isn't a match and the above search_in conditions are also not true
                //that means they put an invalid value in.  add a warning and default
                let outcome: OutputMessages = Object.create(null);
                outcome.alert = OutputAlert.Warning;
                outcome.message = "Condition has an invalid search-in; defaulting to finding-region(0,0)";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);     
                
                search_in = "finding-region(0,0)";
            }
        }

        return search_in;

    }


    /**
     * go through the array of fix_its and make sure each is in the expected format
     * @param loadedRule the rule loaded from the file system
     */
    private validateFixitArray(loadedRule): FixIt[]
    {
        let fix_its: FixIt[] = [];
        if (this.checkValue(loadedRule.fix_its, loadedRule, "array", "", OutputAlert.Info))
        {
            for (let fixit of loadedRule.fix_its)
            {
                fix_its.push(this.validateFixitObject(fixit, loadedRule));
            }
        }
        else if (this.checkValue(loadedRule.fix_it, loadedRule, "array", "", OutputAlert.Info))
        {
            if (loadedRule.fix_it.length > 0)
            {
                this.writeoutNewRules = true;

                for (let fixit of loadedRule.fix_it)
                {
                    fix_its.push(this.validateFixitObject(fixit, loadedRule));
                }

                let outcome: OutputMessages = Object.create(null);
                outcome.alert = OutputAlert.Warning;
                outcome.message = "'fix_it' was renamed 'fix_its' in a schema update.  the rule should be update";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);
            }
            else
            {
                let outcome: OutputMessages = Object.create(null);
                outcome.alert = OutputAlert.Warning;
                outcome.message = "'fix_it was present but empty.  As the schema has changed it should be removed";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);
            }

        }
        return fix_its;
    }

    /**
     * Go through an individual fixit to make sure it is in the expected format
     * @param loadedFixit the fixit currently being inspected
     * @param loadedRule  the rule loaded from the file system that contains the fixit, used to help generate error messages
     */
    private validateFixitObject(loadedFixit, loadedRule): FixIt
    {
        let fixit: FixIt = Object.create(null);
        fixit.name = this.validateFixitName(loadedFixit.name, loadedRule);
        fixit.type = this.validateFixitType(loadedFixit.type, loadedRule);
        let outcome: OutputMessages;

        fixit._comment = (RuleValidator.isSet(loadedFixit._comment, "string")) ? loadedFixit._comment : "";

        //the name of replacement is new, previously having been called replace.  check if the new or old values are present
        if (RuleValidator.isSet(loadedFixit.replacement, "string"))
        {
            fixit.replacement = loadedFixit.replacement;
        }
        else if (this.checkValue(loadedFixit.replace, loadedRule, "string", "fix_its.replacement value is missing from object", OutputAlert.Error))
        {
            fixit.replacement = loadedFixit.replace;

            outcome = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "fix_its.replace has been changed to fix_its.replacement in a schema update.  the rule should be update";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;
            this.outputMessages.push(outcome);
            this.writeoutNewRules = true;
        }

        //the schema was updated to use a full pattern object instead of the old 'search' value.  Check if a pattern is present
        //and if not, then check if search is present and make a pattern out of it
        if (RuleValidator.isSet(loadedFixit.pattern, "Pattern"))
        {
            fixit.pattern = this.validatePatternObject(loadedFixit.pattern, loadedRule);
        }
        else if (this.checkValue(loadedFixit.search, loadedRule, "string", "fix_its.pattern value is missing from object", OutputAlert.Error))
        {
            let pattern: Pattern = Object.create(null);
            pattern.pattern = loadedFixit.search;
            pattern.type = "regex";
            pattern.scopes = ["code"];
            fixit.pattern = pattern;

            outcome = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "fix_its.search has been changed to fix_its.pattern in a schema update.  the rule should be update";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;
            this.outputMessages.push(outcome);
            this.writeoutNewRules = true;
        }
        return fixit;
    }

    /**
     * Ensure the type of fixit matches an expected value
     * @param fixitType either regex-replace or string-replace
     * @param loadedRule rule loaded from File System whose severity is being validated 
     */
    private validateFixitType(fixitType: string, loadedRule): string
    {
        this.checkValue(fixitType, loadedRule, "string", "fix_its.type value is missing from object", OutputAlert.Error);
        fixitType = fixitType.toLowerCase();
        let outcome: OutputMessages = Object.create(null);
        switch (fixitType)
        {
            case "regex-replace":
            case "string-replace":
                return fixitType;

            case "regex-substitute":

                outcome.alert = OutputAlert.Warning;
                outcome.message = "fix_its.type 'regex-substitute' has been changed to 'regex-replace'";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);
                this.writeoutNewRules = true;
                return "regex-replace";

            default:
                outcome.alert = OutputAlert.Error;
                outcome.message = "fix_its.type is not either regex-replace or string-replace";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);
                throw "fix_its.type is not either regex-replace or string-replace";
        }
    }

    /**
     * Make sure the fixit name is present and not too long
     * @param fixitName name of the fixit (e.g. 'Change to strcpy_S')
     * @param loadedRule rule loaded from File System whose severity is being validated 
     */
    private validateFixitName(fixitName: string, loadedRule): string
    {
        this.checkValue(fixitName, loadedRule, "string", "fix_its.name value is missing from object", OutputAlert.Error);
        if (fixitName.length > 128)
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "fix_its.name is more than 128 characters, which is longer than desired";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;

            this.outputMessages.push(outcome);
        }

        return fixitName;
    }

    /**
     * Go through the array of patterns, making sure each pattern object is correctly formed
     * @param loadedRule  rule loaded from File System whose severity is being validated 
     */
    private validatePatternsArray(loadedRule): Pattern[]
    {
        let patterns: Pattern[] = [];
        this.checkValue(loadedRule.patterns, loadedRule, "array", "patterns value is missing from object", OutputAlert.Error);
        if (loadedRule.patterns.length < 1)
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Error;
            outcome.message = "patterns value array is empty";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;
            this.outputMessages.push(outcome);
            throw "patterns value array is empty";
        }
        for (let pattern of loadedRule.patterns)
        {
            patterns.push(this.validatePatternObject(pattern, loadedRule));
        }
        return patterns;
    }

    /**
     * go through the values in pattern and ensure they are present (where required), and in the correct form
     * set any  values that are missing but have defaults we can assume
     * @param loadedPattern the pattern object currently being validated
     * @param loadedRule  rule loaded from File System whose pattern is being validated 
     */
    private validatePatternObject(loadedPattern, loadedRule): Pattern
    {
        let pattern: Pattern = Object.create(null);

        if (this.checkValue(loadedPattern.pattern, loadedRule, "string", "pattern.patten regex value is missing from object", OutputAlert.Error))
        {
            pattern.pattern = loadedPattern.pattern;
        }
        pattern.type = this.validatePatternType(loadedPattern.type, loadedPattern);

        //check if using the scopes array, the old single string value, or if it is absent
        if (RuleValidator.isSet(loadedPattern.scopes, "array") && this.verifyType(loadedPattern.scopes, "array", loadedRule, OutputAlert.Info, ""))
        {
            pattern.scopes = this.validatePatternScopeArray(loadedPattern.scopes, loadedRule);
        }
        else if (RuleValidator.isSet(loadedPattern.scope, "string") && this.verifyType(loadedPattern.scope, "string", loadedRule, OutputAlert.Info, ""))
        {
            //convert to an array if a single value
            let scopes: string[] = [loadedPattern.scope];
            pattern.scopes = this.validatePatternScopeArray(scopes, loadedRule);

            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "pattern.scope has been changed to pattern.scopes, and is now an array.  please update rule";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;
            this.writeoutNewRules = true;
            this.outputMessages.push(outcome);
        }
        else
        {
            pattern.scopes = ["code"];
        }

        pattern._comment = (RuleValidator.isSet(loadedPattern._comment, "string")) ? loadedPattern._comment : "";

        let modifiers: string[] = this.validateStringArray(loadedPattern.modifiers, "pattern.modifiers", loadedRule, this.validateSpecificPatternModifiers);
        if (modifiers.length > 0)
            pattern.modifiers = modifiers;
        return pattern;
    }

    /**
     * check to see if scope is either code, any, or comment, as those are the three allowed values.  If not, return "code"
     * @param scope one of the following values code, any, comment
     * @param loadedRule  rule loaded from File System whose severity is being validated 
     */
    private validatePatternScopeArray(scope: string[], loadedRule): string[]
    {
        let scopes: string[] = this.validateStringArray(scope, "scopes", loadedRule, this.validateSpecificScope);
        if (scopes.length == 0)
            scope.push("code");
        return scopes;
    }

    private validateSpecificScope(scope: string, loadedRule): string
    {
        const outcome: OutputMessages = Object.create(null);
        scope = scope.toLowerCase();
        switch (scope)
        {
            case "code":
            case "any":
            case "comment":
            case "html":
                return scope;

            default:
                outcome.alert = OutputAlert.Warning;
                outcome.message = "pattern.scope has an unrecognized value.  assuming 'code'";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;

                this.outputMessages.push(outcome);
                return "code";
        }
    }

    /**
     * Make sure rules_info is present.  If it is a full URL (legacy schema), chunk it down to just the file name
     * @param loadedRule rule loaded from File System whose severity is being validated 
     */
    private validateRuleInfo(loadedRule): string
    {
        this.checkValue(loadedRule.rule_info, loadedRule, "string", "ruleInfo value is missing from object", OutputAlert.Error);
        let info: string = loadedRule.rule_info;
        let slashIndex: number = info.lastIndexOf("/");
        //check to see if this is a url
        while (slashIndex != -1)
        {
            //better make sure there isn't a trailing slash at the end
            if (slashIndex == info.length - 1)
            {
                info = info.substr(0, info.length - 1);
                slashIndex = info.lastIndexOf("/");

                let outcome: OutputMessages = Object.create(null);
                outcome.alert = OutputAlert.Warning;
                outcome.message = "ruleInfo has a trailing slash";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);
                this.writeoutNewRules = true;

                continue;
            }
            //looks like a url (or at least a path of some form).  chop off everything before the last slash
            //write out a warning, and adopt the remaining value
            info = info.substr(slashIndex + 1);
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "ruleInfo is no longer a full url.  Should just be a filename";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;
            this.outputMessages.push(outcome);
            this.writeoutNewRules = true;
            break;
        }
        return info;
    }

    /**
     * Ensure that the severity is one of the known allowed values
     * @param patternType @todo: ??
     * @param loadedRule rule loaded from File System whose severity is being validated
     */
    private validatePatternType(patternType: string, loadedRule): string
    {
        this.checkValue(patternType, loadedRule, "string", "pattern.type value is missing from object", OutputAlert.Error);
        let outcome: OutputMessages = Object.create(null);

        patternType = patternType.toLowerCase();

        //check if severity is one of the expected values, or a common error that can easily be changed into an expected value
        switch (patternType)
        {
            case "regex":
            case "regex-word":
            case "string":
            case "substring":
                return patternType;

            //the schema used to use _ for multiword values, but now uses -.  check to see if any of those old values are
            //accidentally present
            case "regex_word":
                outcome.alert = OutputAlert.Warning;
                outcome.message = "pattern.type regex_word' should be 'regex-word'";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);

                this.writeoutNewRules = true;
                return "regex-word";
        }

        //if we made it this far, severity isn't any value we recognize, and it needs to be.  Write an error message and throw an exception
        outcome.alert = OutputAlert.Error;
        outcome.message = "Unknown pattern.type in rule.  Please see documentation at https://github.com/microsoft/devskim/wiki";
        outcome.ruleid = loadedRule.id;
        outcome.file = loadedRule.filepath;

        this.outputMessages.push(outcome);
        throw "Unknown pattern.type in rule.  Please see documentation at https://github.com/microsoft/devskim/wiki";
    }

    /**
     * Ensure that the severity is one of the known allowed values
     * @param loadedRule rule loaded from File System whose severity is being validated
     */
    private validateSeverity(loadedRule): string
    {
        this.checkValue(loadedRule.severity, loadedRule, "string", "severity value is missing from object", OutputAlert.Error);
        let outcome: OutputMessages = Object.create(null);

        let severity: string = loadedRule.severity.toLowerCase();

        //check if severity is one of the expected values, or a common error that can easily be changed into an expected value
        switch (severity)
        {
            case "critical":
            case "important":
            case "moderate":
            case "best-practice":
            case "manual-review":
                return severity;

            //the schema used to use _ for multiword values, but now uses -.  check to see if any of those old values are
            //accidentally present
            case "manual_review":
                outcome.alert = OutputAlert.Warning;
                outcome.message = "severities ''manual_review' should be 'manual-review'";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);

                this.writeoutNewRules = true;
                return "manual-review";

            case "best_practice":
                outcome.alert = OutputAlert.Warning;
                outcome.message = "severities ''best_practice' should be 'best-practice' have been replaced with 'best-practice'";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);

                this.writeoutNewRules = true;
                return "best-practice";

            //we rolled low & defense-in-depth into a single "best-practice" level, but there may still be some old rules
            //with the old values
            case "low":
            case "defense-in-depth":
            case "defense_in_depth":
                outcome.alert = OutputAlert.Warning;
                outcome.message = "severities 'low' and 'defense-in-depth' have been replaced with 'best-practice'";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;

                this.outputMessages.push(outcome);

                this.writeoutNewRules = true;
                return "best-practice";
        }

        //if we made it this far, severity isn't any value we recognize, and it needs to be.  Write an error message and throw an exception
        outcome.alert = OutputAlert.Error;
        outcome.message = "Unknown severity in rule.  Please see documentation at https://github.com/microsoft/devskim/wiki";
        outcome.ruleid = loadedRule.id;
        outcome.file = loadedRule.filepath;

        this.outputMessages.push(outcome);
        throw "Unknown severity in rule.  Please see documentation at https://github.com/microsoft/devskim/wiki";
    }

    /**
     * check if name is present (it's required, an exception will be thrown if missing), and if
     * its longer than 64 chars record a warning
     */
    private validateName(loadedRule): string
    {
        this.checkValue(loadedRule.name, loadedRule, "string", "name value is missing from object", OutputAlert.Error);
        if (loadedRule.name.length > 128)
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "name is more than 128 characters, which is longer than desired";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;
            this.outputMessages.push(outcome);
        }
        return loadedRule.name;
    }

    /**
     * check if name is present (it's required, an exception will be thrown if missing), and if
     * its longer than 64 chars record a warning
     */
    private validateRecommendation(loadedRule): string
    {
        let valid: boolean = this.checkValue(loadedRule.recommendation, loadedRule, "string", "recommendation value is missing from object", OutputAlert.Warning);
        if (valid)
        {
            if (loadedRule.recommendation.length > 512)
            {
                let outcome: OutputMessages = Object.create(null);
                outcome.alert = OutputAlert.Warning;
                outcome.message = "recommendation is more than 512 characters, which is longer than desired";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);
            }
            return loadedRule.recommendation;
        }

        //ok, it wasn't the new schema key, maybe it was the old one?
        valid = this.checkValue(loadedRule.replacement, loadedRule, "string", "recommendation value is missing from object", OutputAlert.Warning);

        if (valid)
        {
            this.writeoutNewRules = true;
            if (loadedRule.replacement.length > 512)
            {
                let outcome: OutputMessages = Object.create(null);
                outcome.alert = OutputAlert.Warning;
                outcome.message = "recommendation is more than 512 characters, which is longer than desired";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;

                this.outputMessages.push(outcome);
            }
        }
        return (valid) ? loadedRule.replacement : "";
    }

    /**
     * check if description is present (it's required, an exception will be thrown if missing), and if
     * its longer than 512 chars record a warning
     */
    private validateDescription(loadedRule): string
    {
        this.checkValue(loadedRule.description, loadedRule, "string", "description value is missing from object", OutputAlert.Error);
        if (loadedRule.description.length > 512)
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "description is more than 512 characters, which is longer than desired";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;

            this.outputMessages.push(outcome);
        }

        return loadedRule.description;
    }

    /**
     * check that id is present (its required, an exception will be thrown if missing), and in the form DS######
     */
    private validateID(loadedRule): string
    {
        this.checkValue(loadedRule.id, loadedRule, "string", "id is missing from rule", OutputAlert.Error);
        //if the ID isn't in the expected form, we can still function, but we should write out a warning     
        let idRegex: RegExp = /\b(DS\d\d\d\d\d\d)\b/;
        if (!idRegex.test(loadedRule.id))
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "id is not in the expected format of DS######";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;

            this.outputMessages.push(outcome);
        }
        return loadedRule.id;
    }


    /**
     * Validates the various optional arrays of strings (applies_to, tags, overrides), since the logic was mostly the same
     * for all of them.  The different logic comes up with validating the individual values, and a callback function is used for that
     * @param arrayToValidate the array being inspected
     * @param arrayName the name of the array object - applies_to, tags, etc.
     * @param loadedRule the rule currently being scrutinized.  This is used to create any output messages.  It would be more performant to
     *   pass just the ruleID and file, so consider refactoring.  Unfortunately I was lazy, and a ton of functions already take this
     * @param stringValidator function to validate the individual strings in the array, since this is the point of variance between tags, applies_to, etc.
     * the function should accept a string and loadedRule as params, and return a string (in case it needs to do any cleanup)
     */
    private validateStringArray(arrayToValidate: string[], arrayName: string, loadedRule, stringValidator): string[]
    {
        let stringArray: string[] = [];
        //check if the value is present.  It isn't required, so it is fine if absent
        //if absent, just return an empty array
        if (RuleValidator.isSet(arrayToValidate, "array"))
        {
            if (this.verifyType(arrayToValidate, "array", loadedRule, OutputAlert.Warning, arrayName + " is not an array"))
            {
                //since these arrays are all optional, it is silly to include an empty version of them
                if (arrayToValidate.length < 1)
                {
                    let outcome: OutputMessages = Object.create(null);
                    outcome.alert = OutputAlert.Info;
                    outcome.message = arrayName + " is an empty array and can be left out";
                    outcome.ruleid = loadedRule.id;
                    outcome.file = loadedRule.filepath;

                    this.outputMessages.push(outcome);
                }
                else
                {
                    for (let strings of arrayToValidate)
                    {
                        if (this.checkValue(strings, loadedRule, "string", "a value of type other than string present in " + arrayName, OutputAlert.Warning))
                        {
                            //validate the individual string
                            strings = stringValidator(strings, loadedRule);
                            stringArray.push(strings);
                        }
                    }
                }

            }
        }
        return stringArray;
    }

    /**
     * Ensures that a pattern modifier is one of the regular expression modifiers
     * @param modifier a regex modifier
     * @param loadedRule the rule currently being scrutinized.  This is used to create any output messages.  It would be more performant to
     *   pass just the ruleID and file, so consider refactoring.  Unfortunately I was lazy, and a ton of functions already take this 
     */
    private validateSpecificPatternModifiers(modifier: string, loadedRule): string
    {
        let outcome: OutputMessages = Object.create(null);
        modifier = modifier.toLowerCase();
        switch (modifier)
        {
            case "i":
            case "d":
            case "m":
                return modifier;

            default:
                outcome.alert = OutputAlert.Warning;
                outcome.message = "Unknown modifier in pattern.";
                outcome.ruleid = loadedRule.id;
                outcome.file = loadedRule.filepath;
                this.outputMessages.push(outcome);
                return "";
        }
    }

    /**
     * validator function for each string in the overrides array.  checks to make sure the format of the string is DS######
     * (that it is a ruleID), and writes a warning to the log if it isn't.
     * @param overridden ruleID for the rule that was overridden
     * @param loadedRule the rule currently being scrutinized.  This is used to create any output messages.  It would be more performant to
     *   pass just the ruleID and file, so consider refactoring.  Unfortunately I was lazy, and a ton of functions already take this
     */
    private validateSpecificOverride(overridden: string, loadedRule): string
    {
        let idRegex: RegExp = /\b(DS\d\d\d\d\d\d)\b/;
        if (!idRegex.test(overridden))
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = OutputAlert.Warning;
            outcome.message = "override is not in the expected format of DS######";
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;

            this.outputMessages.push(outcome);
        }
        return overridden;
    }

    private static validateSpecificapplies_to(applies: string, loadedRule): string
    {
        //TODO: logic to validate applies
        return applies;
    }

    private static validateSpecificTags(tags: string, loadedRule): string
    {
        //TODO: logic to validate applies
        return tags;
    }

    /**
     * Check that a required value is present, and if a string, longer than 0 chars. 
     * If it isn't, record an error message (which eventually gets written to file),
     * and throw an exception
     * @param variable value we are testing
     * @param loadedRule the loaded rule object the value came from (used to write error message)
     * @param varType types to check for: string, array, boolean, or number 
     * @param errorMessage message to write out if object is missing
     * @param alertLevel warning level from OutputAlert.  If set to OutputAlert.Error and value doesn't match, throws exception
     */
    private checkValue(variable, loadedRule, varType: string, errorMessage: string, alertLevel: string)
    {
        if (!RuleValidator.isSet(variable, varType) && errorMessage.length > 0)
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = alertLevel;
            outcome.message = errorMessage;
            //use the ID so we know which rule is broken, or another error message if ID is missing
            outcome.ruleid = RuleValidator.isSet(loadedRule.id, "string") ? loadedRule.id : "ID NOT FOUND";
            //this is generated when the object is read in, so should reliably be present
            outcome.file = loadedRule.filepath;

            this.outputMessages.push(outcome);

            if (alertLevel == OutputAlert.Error)
                throw errorMessage;
        }
        return this.verifyType(variable, varType, loadedRule, alertLevel, errorMessage);
    }

    /**
     * returns true if the variable has a value, false if it is undefined/null/zero length
     * @param variable value to check 
     * @param varType types to check for: string, array, boolean, or number 
     */
    private static isSet(variable, varType: string): boolean
    {
        return !(!variable || ((varType == "string" || varType == "array") && variable.length < 1));
    }

    /**
     * 
     * @param variable value being tested
     * @param varType types to check for: string, array, boolean, or number 
     * @param loadedRule rule object we are checking value from, used to construct error message if check fails
     * @param alertLevel warning level from OutputAlert.  If set to OutputAlert.Error and value doesn't match, throws exception
     * @param warningMessage message to write in the error message, and exception (if alertLevel is "Error")
     */
    private verifyType(variable, varType: string, loadedRule, alertLevel: string, warningMessage: string): boolean
    {
        let verifiedType = true;
        switch (varType)
        {
            case "string":
                if (!(typeof variable === 'string' || variable instanceof String))
                {
                    verifiedType = false;
                }
                break;
            case "number":
                if (!(typeof variable === 'number' || Number.isInteger(Number.parseInt(variable.toString(),10))))
                {
                    verifiedType = false;
                }
                break;
            case "boolean":
                if (typeof variable !== 'boolean')
                {
                    verifiedType = false;
                }
                break;
            case "array":
                if (!(Array.isArray(variable) || variable instanceof Array))
                {
                    verifiedType = false;
                }
                break;
        }

        if (!verifiedType && warningMessage.length > 0)
        {
            let outcome: OutputMessages = Object.create(null);
            outcome.alert = alertLevel;
            outcome.message = warningMessage;
            outcome.ruleid = loadedRule.id;
            outcome.file = loadedRule.filepath;

            this.outputMessages.push(outcome);
            if (alertLevel == OutputAlert.Error)
            {
                throw warningMessage;
            }
        }
        return verifiedType;
    }
}

/**
 * Object used to store warning and error messages that occur while validating the rules.  If the user has
 * the validateRulesFiles setting enabled, this will be output to the directory the extension lives in
 */
export interface OutputMessages
{
    file: string;
    ruleid: string;
    alert: string;
    message: string;
}

/**
 * An enum would normally be better for this sort of thing BUT I want to 
 * ultimately write a string rather than in integer out in the JSON these
 * values are destined for
 */
export class OutputAlert
{
    public static Error: string = "Error";
    public static Warning: string = "Warning";
    public static Info: string = "Info";
    public static Success: string = "Success";
}