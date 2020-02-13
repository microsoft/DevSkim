import {DevSkimSuppression} from "../src/utility_classes/suppressions";
import {DevskimRuleSeverity, DevSkimSettings, IDevSkimSettings} from "../src/devskimObjects";

jest.mock("../src/devskimObjects");

describe('DevSkimSuppression', () => {
    let mockedSettings: IDevSkimSettings;
    let dsSuppression: DevSkimSuppression;

    beforeAll(() => {
        mockedSettings = new DevSkimSettings();
        mockedSettings.suppressionDurationInDays = 3;
        mockedSettings.manualReviewerName = 'Kitzmiller';
        dsSuppression = new DevSkimSuppression(mockedSettings);
    });

    it('construct is called with mocked settings', () => {
        expect(dsSuppression).toBeTruthy();
        expect(dsSuppression.dsSettings.manualReviewerName).toEqual('Kitzmiller');
        expect(dsSuppression.dsSettings.suppressionDurationInDays).toEqual(3);
    });

    it('createActions ', () => {
        let ruleID = 'DS189424';
        let startCharacter = 77;
        let lineStart = 5;
        let langID = 'javascript';
        let ruleSeverity = DevskimRuleSeverity.ManualReview;

        let fixEdits = dsSuppression.createActions(ruleID, mockDocuments[0], startCharacter, lineStart, langID, ruleSeverity);
        expect(fixEdits).toBeTruthy();
        expect(fixEdits[0].fixName).toContain('Mark DS189424 as Reviewed');
        expect(fixEdits[0].text).toContain('reviewed');
        expect(fixEdits[0].range.start.line).toBe(5);
        expect(fixEdits[0].range.start.character).toBe(44);
        expect(fixEdits[0].range.end.line).toBe(5);
        expect(fixEdits[0].range.end.character).toBe(45);
    });

    it('createActions 2 ', () => {
        let ruleID = 'DS189424';
        let startCharacter = 77;
        let lineStart = 5;
        let langID = 'javascript';
        let ruleSeverity = DevskimRuleSeverity.ManualReview;

        let fixEdits = dsSuppression.createActions(ruleID, mockDocuments[1], startCharacter, lineStart, langID, ruleSeverity);
        expect(fixEdits).toBeTruthy();
        expect(fixEdits[0].fixName).toContain('Mark DS189424 as Reviewed');
        expect(fixEdits[0].text).toContain('reviewed');
        expect(fixEdits[0].range.start.line).toBe(5);
        expect(fixEdits[0].range.start.character).toBe(44);
        expect(fixEdits[0].range.end.line).toBe(5);
        expect(fixEdits[0].range.end.character).toBe(45);
    });

});

const mockDocuments = [`
// 1
console.log('section 1');

// 2
console.log('section 2');
let e1 = eval('find . -name "exec" -print');

// 3
let e3 = 3;

`,
`
// 1
console.log('section 1');

// 2
console.log('section 2');
let e1 = eval('find . -name "exec" -print');

// 3
console.log('section 2');

`];
