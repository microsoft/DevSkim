import * as vscode from 'vscode';
import { CodeFixMapping } from './common/codeFixMapping';
import { DevSkimSettings, DevSkimSettingsObject } from './common/devskimSettings';
import { ExtensionToCodeCommentStyle } from './common/languagesAccess';

export class DevSkimFixer implements vscode.CodeActionProvider {

	fixMapping = new Map<string, string[]>();
	config = new DevSkimSettingsObject();
	public static readonly providedCodeActionKinds = [
		vscode.CodeActionKind.QuickFix
	];

	setConfig(config: DevSkimSettings)
	{
		this.config = config;
		this.fixMapping = new Map<string, string[]>();
	}

	createMapKeyForDiagnostic(diagnostic: vscode.Diagnostic, fileName: string) : string
	{
		return `${fileName}: ${diagnostic.message}, ${String(diagnostic.code)}, ${diagnostic.range.start.line}, ${diagnostic.range.start.character}, ${diagnostic.range.end.line}, ${diagnostic.range.end.character}`;
	}
	
	ensureMapHasMapping(mapping: CodeFixMapping)
	{
		const key = this.createMapKeyForDiagnostic(mapping.diagnostic, mapping.fileName);
		const keyVal = this.fixMapping.get(key);
		if(keyVal != undefined)
		{
			if (!keyVal.find(x => x == mapping.replacement))
			{
				this.fixMapping.set(key, keyVal.concat(mapping.replacement));
			}
		}
		else
		{
			this.fixMapping.set(key, [mapping.replacement]);
		}
	}

	provideCodeActions(document: vscode.TextDocument, range: vscode.Range | vscode.Selection, context: vscode.CodeActionContext, token: vscode.CancellationToken): vscode.CodeAction[] {
		// for each diagnostic entry that has the matching `code`, create a code action command
		const output : vscode.CodeAction[] = [];
		context.diagnostics.filter(diagnostic => String(diagnostic.code).startsWith("MS-CST-E.vscode-devskim")).forEach((filteredDiagnostic : vscode.Diagnostic) => {
			// The ToString method on URI in node swaps ':' into '%3A', but the C# one does not, but we need them to match.
			this.fixMapping.get(this.createMapKeyForDiagnostic(filteredDiagnostic, document.uri.toString().replace("%3A", ":")))?.forEach(codeFix => {
				output.push(this.createFix(document, filteredDiagnostic, codeFix));
			});
		});

		return output;
	}

	private createFix(document: vscode.TextDocument, diagnostic: vscode.Diagnostic, codeFix: string): vscode.CodeAction 
	{
		// If DevSkim ignore is in the line, this appears to be a suppression
		const index = codeFix.indexOf("DevSkim: ignore ");
		// DevSkim: Ignore is 16 characters
		// Give the user the correct action message depending on if this is a suppression or fix
		const fixString = (index > -1) ? `Suppress ${codeFix.substring(index+16)}` : `Replace with ${codeFix}`
		const fix = new vscode.CodeAction(fixString, vscode.CodeActionKind.QuickFix);
		fix.edit = new vscode.WorkspaceEdit();
		fix.edit.replace(document.uri, diagnostic.range, codeFix);
		return fix;
	}
}