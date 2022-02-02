/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

interface CodeCommentsEntry{
	languages: string[];
	inline?: string;
	prefix?: string;
	suffix?: string;
}

interface LanguageEntry
{
	name: string;
	extensions: string[];
}

import languageList from './languages.json';
import commentsList from './comments.json';

const extensionToCodeCommentStyleMap = new Map<string, CodeCommentsEntry>();

languageList.forEach(languageEntry => {
	const commentsEntry = commentsList.filter(y => y.languages.includes(languageEntry.name))[0];
	languageEntry.extensions.forEach(extension => {
		extensionToCodeCommentStyleMap.set(extension.startsWith('.') ? extension.substring(1) : extension, commentsEntry);
	});
});

export function ExtensionToCodeCommentStyle(extension: string)
{
	return extensionToCodeCommentStyleMap.get(extension);
}