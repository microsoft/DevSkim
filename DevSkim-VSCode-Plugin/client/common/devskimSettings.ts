/* eslint-disable @typescript-eslint/no-inferrable-types */
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export interface DevSkimSettings {
    enableManualReviewRules: boolean;
    enableBestPracticeRules: boolean;
    enableUnspecifiedSeverityRules: boolean;
    enableLowConfidenceRules: boolean;
    enableUnspecifiedConfidenceRules: boolean;
    suppressionDurationInDays: number;
    suppressionCommentStyle: string;
    manualReviewerName: string;
    ignoreFiles: string[];
    ignoreRulesList: string;
    guidanceBaseURL: string;
    removeFindingsOnClose: boolean;
    ignoreDefaultRules: boolean;
    customRulesPaths: string[];
    customLanguagesPath: string;
    customCommentsPath: string;
    scanOnOpen: boolean;
    scanOnSave: boolean;
    scanOnChange: boolean;
    traceServer: boolean;
}

export class DevSkimSettingsObject implements DevSkimSettings {
    enableBestPracticeRules: boolean = false;
    enableManualReviewRules: boolean = false;
    enableUnspecifiedSeverityRules: boolean = false;
    enableLowConfidenceRules: boolean = false;
    enableUnspecifiedConfidenceRules: boolean = false;
    suppressionDurationInDays: number = 30;
    suppressionCommentStyle: string = "line";
    manualReviewerName: string = "";
    ignoreFiles: string[] = [];
    ignoreRulesList: string = "";
    guidanceBaseURL: string = "https://github.com/Microsoft/DevSkim/Guidance";
    removeFindingsOnClose: boolean = false;
    ignoreDefaultRules: boolean = false;
    customRulesPaths: string[] = [];
    customLanguagesPath: string = "";
    customCommentsPath: string = "";
    scanOnOpen: boolean = true;
    scanOnSave: boolean = true;
    scanOnChange: boolean = true;
    traceServer: boolean = false;
}