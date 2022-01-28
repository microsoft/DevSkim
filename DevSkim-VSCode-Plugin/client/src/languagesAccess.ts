/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

interface CodeCommentsEntry{
	language: string[];
	inline?: string;
	prefix?: string;
	suffix?: string;
}

interface LanguagesEntry
{
	name: string;
	extensions: string[];
}

const languageList : LanguagesEntry[] = JSON.parse(`[
	{
	  "name": "c",
	  "extensions": [ ".c", ".h" ]
	},
	{
	  "name": "cpp",
	  "extensions": [ ".cpp", ".hpp", ".cc", ".hh", ".cxx", ".hxx", ".inl", ".h" ]
	},
	{
	  "name": "csharp",
	  "extensions": [ ".cs", ".cshtml", ".razor" ]
	},
	{
	  "name": "vb",
	  "extensions": [ ".vb" ]
	},
	{
	  "name": "python",
	  "extensions": [ ".py" ]
	},
	{
	  "name": "javascript",
	  "extensions": [ ".js" ]
	},
	{
	  "name": "javascriptreact",
	  "extensions": [ ".jsx" ]
	},
	{
	  "name": "typescript",
	  "extensions": [ ".ts" ]
	},
	{
	  "name": "typescriptreact",
	  "extensions": [ ".tsx" ]
	},
	{
	  "name": "coffeescript",
	  "extensions": [ ".coffee" ]
	},
	{
	  "name": "java",
	  "extensions": [ ".java" ]
	},
	{
	  "name": "objective-c",
	  "extensions": [ ".m" ]
	},
	{
	  "name": "swift",
	  "extensions": [ ".swift" ]
	},
	{
	  "name": "perl",
	  "extensions": [ ".pl", ".pm", ".t", ".pod" ]
	},
	{
	  "name": "perl6",
	  "extensions": [ ".pl6", ".p6", ".pm6" ]
	},
	{
	  "name": "ruby",
	  "extensions": [ ".rb" ]
	},
	{
	  "name": "lua",
	  "extensions": [ ".lua" ]
	},
	{
	  "name": "groovy",
	  "extensions": [ ".groovy" ]
	},
	{
	  "name": "go",
	  "extensions": [ ".go" ]
	},
	{
	  "name": "rust",
	  "extensions": [ ".rs" ]
	},
	{
	  "name": "jade",
	  "extensions": [ ".jade" ]
	},
	{
	  "name": "clojure",
	  "extensions": [ ".clj", ".cljs", ".cljc", ".edn" ]
	},
	{
	  "name": "r",
	  "extensions": [ ".r" ]
	},
	{
	  "name": "yaml",
	  "extensions": [ ".yaml", ".yml" ]
	},
	{
	  "name": "fsharp",
	  "extensions": [ ".fs" ]
	},
	{
	  "name": "php",
	  "extensions": [ ".php" ]
	},
	{
	  "name": "powershell",
	  "extensions": [ ".ps1", ".psm1", ".psd1" ]
	},
	{
	  "name": "shellscript",
	  "extensions": [ ".sh" ]
	},
	{
	  "name": "sql",
	  "extensions": [ ".sql" ]
	},
	{
	  "name": "plaintext",
	  "extensions": [ ".txt" ]
	},
	{
	  "name": ".config",
	  "extensions": [ ".config" ]
	},
	{
	  "name": "packages.config",
	  "extensions": [ "packages.config" ]
	},
	{
	  "name": "CSharp Project",
	  "extensions": [ ".csproj" ]
	},
	{
	  "name": "cobol",
	  "extensions": [ ".cbl", ".cob", ".cpy" ]
	},
	{
	  "name": "json",
	  "extensions": [ ".json" ]
	}
  ]`);

const commentsList : CodeCommentsEntry[] = JSON.parse(`
[
	{
	  "language": [
		"c",
		"cpp",
		"csharp",
		"coffeescript",
		"fsharp",
		"go",
		"groovy",
		"jade",
		"objective-C",
		"rust",
		"swift",
		"javascript",
		"java",
		"typescript",
		"php"
	  ],
	  "inline": "//",
	  "prefix": "/*",
	  "suffix": "*/"
	},
	{
	  "language": [
		"plaintext"
	  ],
	  "always":  true
	},
	{
	  "language": [
		"perl",
		"perl6",
		"r",
		"shellscript",
		"ruby",
		"yaml",
		"powershell",
		"python"
	  ],
	  "inline": "#",
	  "prefix": "#",
	  "suffix": "\\n"
	},
	{
	  "language": [
		"lua",
		"sql"
	  ],
	  "inline": "--",
	  "prefix": "--",
	  "suffix": "\\n"
	},
	{
	  "language": [
		"clojure"
	  ],
	  "inline": ";;",
	  "prefix": ";;",
	  "suffix": ""
	},
	{
	  "language": [
		"vb"
	  ],
	  "inline": "'",
	  "prefix": "'",
	  "suffix": ""
	}
  ]`);

const extensionToCodeCommentStyleMap = new Map<string, CodeCommentsEntry>();

languageList.forEach(languageEntry => {
	const commentsEntry = commentsList.filter(y => y.language.includes(languageEntry.name))[0];
	languageEntry.extensions.forEach(extension => {
		extensionToCodeCommentStyleMap.set(extension.startsWith('.') ? extension.substring(1) : extension, commentsEntry);
	});
});

export function ExtensionToCodeCommentStyle(extension: string)
{
	return extensionToCodeCommentStyleMap.get(extension);
}