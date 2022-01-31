import {
	Diagnostic
} from 'vscode-languageserver/node';
export class CodeFixMapping
{
	diagnostic: Diagnostic;
	replacement: string;
	fileName: string;
	constructor(diagnostic: Diagnostic, replacement: string, fileName: string)
	{
		this.diagnostic = diagnostic;
		this.replacement = replacement;
		this.fileName = fileName;
	}
}