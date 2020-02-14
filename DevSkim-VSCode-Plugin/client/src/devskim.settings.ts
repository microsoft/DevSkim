export interface DevSkimSettings {
    enableManualReviewRules: boolean;
    enableInformationalSeverityRules: boolean;
    enableDefenseInDepthSeverityRules: boolean;
    enableBestPracticeRules: boolean;
    enableLowSeverityRules: boolean;
    suppressionDurationInDays: number;
    manualReviewerName: string;
    ignoreFiles: string[];
    ignoreRulesList: string[];
    validateRulesFiles: boolean;
    guidanceBaseURL: string;
    removeFindingsOnClose: boolean;
    analyzeMode: string;
    maxFileSizeKB : number;
}

export class DevSkimSettingsObject implements DevSkimSettings {
    enableBestPracticeRules: boolean;
    enableDefenseInDepthSeverityRules: boolean;
    enableInformationalSeverityRules: boolean;
    enableLowSeverityRules: boolean;
    enableManualReviewRules: boolean;
    guidanceBaseURL: string;
    ignoreFiles: string[];
    ignoreRulesList: string[];
    manualReviewerName: string;
    removeFindingsOnClose: boolean;
    suppressionDurationInDays: number;
    validateRulesFiles: boolean;
    analyzeMode: string;
    maxFileSizeKB : number;
};