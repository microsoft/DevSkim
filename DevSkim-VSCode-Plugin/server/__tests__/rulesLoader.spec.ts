const path = require('path');
import {Connection} from 'vscode-languageserver';
jest.mock('vscode-languageserver');
import {RulesLoader} from '../src/utility_classes/rulesLoader';

describe('RulesLoader', () => {

    const connection: any = { console: { log: (s) => (console.log(s)) } } as Connection;

    it('will read 81 rules', async () => {
        const ruleDir = path.join(__dirname, "../data/rules");
        const loader = new RulesLoader(connection, true, ruleDir);
        const rules = await loader.loadRules();
        expect(rules.length).toEqual(82);
    });
});