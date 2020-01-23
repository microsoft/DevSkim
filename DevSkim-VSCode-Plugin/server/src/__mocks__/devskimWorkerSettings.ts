import {IDevSkimSettings} from "../devskimObjects";

export class DevSkimWorkerSettings {

    getSettings(settings?: IDevSkimSettings): IDevSkimSettings {
        return this.defaultSettings();
    }

    getRulesDirectory(): string {
        return '';
    }

    defaultSettings(): IDevSkimSettings {

        let obj = Object.create(null);
        Object.assign(obj, {
                "enableBestPracticeRules": true,
                "enableDefenseInDepthSeverityRules": false,
                "enableInformationalSeverityRules": false,
                "enableLowSeverityRules": false,
                "enableManualReviewRules": true,
                "guidanceBaseURL": "https://github.com/Microsoft/DevSkim/blob/master/guidance/",
                "ignoreFiles":
                    [
                        "out/*", "bin/*", "node_modules/*", ".vscode/*", "yarn.lock",
                        "logs/*", "*.log", "*.git", "rulesValidationLog.json",
                    ],
                "ignoreRulesList": [],
                "manualReviewerName": "",
                "removeFindingsOnClose": true,
                "suppressionDurationInDays": 30,
                "validateRulesFiles": true,
            });
        return obj;
    }
}