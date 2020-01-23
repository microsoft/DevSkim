import {DevSkimRules} from "./devskimRules";
import {DevSkimWorkerSettings} from "../src/devskimWorkerSettings";
import {RuleValidator} from "../src/utility_classes/ruleValidator";
import {Connection, IConnection} from "vscode-languageserver";
import {IDevSkimSettings} from "../src/devskimObjects";

jest.mock("../src/utility_classes/ruleValidator");
// jest.mock("./devskimWorkerSettings");
jest.mock('vscode-languageserver', )

const connection: any = { console: { log: (s) => (console.log(s)) } } as Connection;

describe('DevSkimRules', () => {
    let mockedRuleValidator: RuleValidator;
    let mockedSettings: IDevSkimSettings;
    let dsr: DevSkimRules;
    let rd: string;

    beforeAll(() => {
        rd = DevSkimWorkerSettings.getRulesDirectory(connection);
        mockedRuleValidator = new RuleValidator(null, '', '');
    });

    it('is created', async () => {
        dsr = new DevSkimRules(connection, mockedSettings, mockedRuleValidator);
        expect(dsr.rulesDirectory).toBe(rd);
    });
});