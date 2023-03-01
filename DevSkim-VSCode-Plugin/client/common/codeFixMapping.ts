export class CodeFixMapping
{
	// The Diagnostic Type is declared from a different context but we can pass them this way
	diagnostic: any;
	replacement: string;
	fileName: string;
	friendlyString: string;
	constructor(diagnostic: any, replacement: string, fileName: string, friendlyString: string)
	{
		this.diagnostic = diagnostic;
		this.replacement = replacement;
		this.fileName = fileName;
		this.friendlyString = friendlyString;
	}
}