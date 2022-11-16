import * as vscode from 'vscode';
import { CodeFixMapping } from '../common/codeFixMapping';
import { DevSkimSettings, DevSkimSettingsObject } from '../common/devskimSettings';
import { ExtensionToCodeCommentStyle } from '../common/languagesAccess';

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
		return `${fileName}: ${diagnostic.message}, ${diagnostic.code?.valueOf()}, ${diagnostic.range.start.line}, ${diagnostic.range.start.character}, ${diagnostic.range.end.line}, ${diagnostic.range.end.character}`;
	}
	
	ensureMapHasMapping(mapping: CodeFixMapping)
	{
		const key = this.createMapKeyForDiagnostic(mapping.diagnostic, mapping.fileName);
		const keyVal = this.fixMapping.get(key);
		if(keyVal != undefined)
		{
			if (!keyVal.find(x => x = mapping.replacement))
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
		context.diagnostics.filter(diagnostic => diagnostic.code === "MS-CST-E.vscode-devskim").forEach((filteredDiagnostic : vscode.Diagnostic) => {
			this.fixMapping.get(this.createMapKeyForDiagnostic(filteredDiagnostic, document.uri.toString()))?.forEach(codeFix => {
				output.push(this.createFix(document, filteredDiagnostic.range, codeFix));
			});
			const suppression = this.createSuppression(document, filteredDiagnostic.range, filteredDiagnostic);
			if (suppression != null)
			{
				output.push(suppression);
			}
		});

		return output;
	}

	private createSuppression(document: vscode.TextDocument, range: vscode.Range, diagnostic: vscode.Diagnostic): vscode.CodeAction | null
	{
		const issueNum = diagnostic.source?.split(new RegExp('[\\[\\]]'))[1];
		if (issueNum != undefined)
		{
			const extension = document.uri.path.split('.').pop()?.toLowerCase() ?? '';
			const commentStyle = ExtensionToCodeCommentStyle(extension);
			if (commentStyle != undefined)
			{
				const fix = new vscode.CodeAction(`Suppress ${issueNum} finding`, vscode.CodeActionKind.QuickFix);
				fix.edit = new vscode.WorkspaceEdit();
				const text = document.lineAt(range.end.line);
				const reviewer = this.config.manualReviewerName != '' ? ` by ${this.config.manualReviewerName}` : '';
				if (this.config.suppressionCommentStyle == "block" && commentStyle.prefix != undefined && commentStyle.suffix != undefined)
				{
					fix.edit.insert(document.uri, new vscode.Position(range.end.line, text.range.end.character), ` ${commentStyle.prefix} DevSkim: Ignore ${issueNum}${reviewer} ${commentStyle.suffix}`);
				}
				else
				{
					fix.edit.insert(document.uri, new vscode.Position(range.end.line, text.range.end.character), ` ${commentStyle.inline} DevSkim: Ignore ${issueNum}${reviewer}`);
				}
				return fix;
			}
		}
		return null;
	}

	private createFix(document: vscode.TextDocument, range: vscode.Range, codeFix: string): vscode.CodeAction 
	{
		const fix = new vscode.CodeAction(`Replace with ${codeFix}`, vscode.CodeActionKind.QuickFix);
		fix.edit = new vscode.WorkspaceEdit();
		fix.edit.replace(document.uri, range, codeFix);
		return fix;
	}
}