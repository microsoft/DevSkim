/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

// This file has static names for the notifications passed between client and server.

export function getDotNetPath() : string 
{
	return "devskim/dotnetPath";
}

export function getDevSkimPath() : string 
{
	return "devskim/devskimpath";
}

export function getSetSettings() : string
{
	return "devskim/setsettings";
}

export function getCodeFixMapping() : string
{
	return "devskim/codefixmapping";
}