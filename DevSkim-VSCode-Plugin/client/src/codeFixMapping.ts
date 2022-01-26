import * as vscode from 'vscode';

export class CodeFixMapping
{
	diagnostic: vscode.Diagnostic;
	replacement: CodeFix;
	fileName: string;
	constructor(diagnostic: vscode.Diagnostic, replacement: CodeFix, fileName: string)
	{
		this.diagnostic = diagnostic;
		this.replacement = replacement;
		this.fileName = fileName;
	}
}

export interface CodeFix
{
	type: string;
	name: string;
	replacement: string;
}