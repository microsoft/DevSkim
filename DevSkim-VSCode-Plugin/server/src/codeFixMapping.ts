import {
	Diagnostic
} from 'vscode-languageserver/node';
export class CodeFixMapping
{
	diagnostic: Diagnostic;
	replacement: CodeFix;
	fileName: string;
	constructor(diagnostic: Diagnostic, replacement: CodeFix, fileName: string)
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