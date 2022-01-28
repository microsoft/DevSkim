/* eslint-disable @typescript-eslint/no-inferrable-types */
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export interface DevSkimSettings {
    enableManualReviewRules: boolean;
    enableBestPracticeRules: boolean;
    suppressionDurationInDays: number;
    manualReviewerName: string;
    ignoreFiles: string[];
    ignoreRulesList: string;
    guidanceBaseURL: string;
    removeFindingsOnClose: boolean;
    suppressionCommentStyle: string;
}

export class DevSkimSettingsObject implements DevSkimSettings {
    enableBestPracticeRules: boolean = false;
    enableManualReviewRules: boolean = false;
    guidanceBaseURL: string = "https://github.com/Microsoft/DevSkim/Guidance";
    ignoreFiles: string[] = [];
    ignoreRulesList: string = "";
    manualReviewerName: string = "";
    removeFindingsOnClose: boolean = false;
    suppressionDurationInDays: number = 30;
    suppressionCommentStyle: string = "line";
}