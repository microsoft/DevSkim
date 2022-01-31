import * as vscode from 'vscode';

export class CodeFixMapping
{
	diagnostic: vscode.Diagnostic;
	replacement: string;
	fileName: string;
	constructor(diagnostic: vscode.Diagnostic, replacement: string, fileName: string)
	{
		this.diagnostic = diagnostic;
		this.replacement = replacement;
		this.fileName = fileName;
	}
}