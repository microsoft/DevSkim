/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { noop } from '@babel/types';
/**
 * Various helper functions around paths and file types
 */
export class PathOperations
{
    /**
     * check if the current file path meets any of the conditions that should cause analysis
     * to skip it
     * @param filePath the full file path of the file being analyzed 
     * @param ignoreList the current collection of conditions to ignore (e.g. *.log, node_modules/*)
     * @return true if the file should be ignored, false if it should be processed
     */
    public static ignoreFile(filePath: string, ignoreList: string[] = []): boolean
    {        
        if (filePath && filePath.length > 1)
        {
            filePath = filePath.replace(/\\/g,"/");
            let XRegExp = require('xregexp');
            for(let ignore of ignoreList)
            {
                //if the string representation of the regex in the array is screwed up, it may
                //cause an exception when its used to construct a regex.  That shouldn't prohibit
                //trying the other patterns
                try{
                    if(XRegExp.exec(filePath, RegExp(ignore, "i")))
                    {
                        return true;
                    }
                }
                catch(e) {noop();}
            }      
        }

        return false;
    }

    /**
     * Clean up the directory path, stripping trailing / or \ and replacing double slashes with single
     * even if not cleaned up, it should work with every API that needs a path, but it makes path comparisons suck
     * @param directory the directory path to clean up
     * @return cleaned up directory path
     */
    public normalizeDirectoryPaths(directory : string) : string
    {
        directory = directory.replace(/\\\\/g,"\\" );
        directory = directory.replace(/\/\//g,"/" );
        if(directory.slice(-1) == "\\" || directory.slice(-1) == "/" )
        {
            directory = directory.substring(0,directory.length );
        }

        // Chop off any initial ./ or .\ substrings
        if (directory.slice(0, 2) === './' || directory.slice(0, 2) === '.\\') {
            directory = directory.slice(2);
        }
        return directory;        
    }


    /**
     * Figure out the mime type for a source file based on its extension.  this is using a precomputed list of 
     * mime types, so will not succeed for every file type
     * @param filePath the path of the file 
     * @return the mime type of the file if known, otherwise text/plain 
     */
    public getMimeFromPath(filePath : string) : string
    {
        let path = require('path');

        let extension : string = path.extname(filePath);
        switch (extension.toLowerCase())
        {
            case ".bat":
            case ".cmd": return "text/x-c";

            case ".clj":
            case ".cljs":
            case ".cljc":
            case ".cljx":
            case ".clojure":
            case "edn": return "text/plain";

            case ".coffee":
            case ".cson": return "text/plain";

            case ".c": return "text/x-c";

            case ".cpp":
            case ".cc":
            case ".cxx":
            case ".c++":
            case ".h": 
            case ".hh":
            case ".hxx":
            case ".hpp":
            case ".h++": return "text/x-c";

            case ".cs":
            case ".csx":
            case ".cake": return "text/plain";

            case ".css": return "text/css";

            case ".dockerfile": return "text/plain";

            case ".fs":
            case ".fsi":
            case ".fsx":
            case ".fsscript": return "text/plain";

            case ".go": return "text/plain";

            case ".groovy": 
            case ".gvy": 
            case ".gradle": return "text/plain";

            case ".handlebars":
            case ".hbs": return "text/plain";

            case ".hlsl":
            case ".fx":
            case ".fxh":
            case ".vsh":
            case ".psh":
            case ".cfinc":
            case ".hlsli": return "text/plain";

            case ".html":
            case ".htm":
            case ".shtml":
            case ".xhtml":
            case ".mdoc":
            case ".jsp":
            case ".asp":
            case ".aspx":
            case ".jshtm":
            case ".volt":
            case ".ejs":
            case ".rhtml": return "text/html";

            case ".ini": return "text/plain";

            case ".jade": 
            case ".pug": return "jade";

            case ".java": 
            case ".jav": return "text/plain";

            case ".jsx": return "text/javascript";

            case ".js":
            case ".es6":
            case "mjs":
            case ".pac": return "text/javascript";

            case ".json":
            case ".bowerrc":
            case ".jshintrc":
            case ".jscsrc":
            case ".eslintrc":
            case ".babelrc":
            case ".webmanifest":
            case ".code-workspace": return "application/json";

            case ".less": return "text/plain";

            case ".lua": return "text/plain";

            case ".mk": return "text/plain";

            case ".md":
            case ".mdown":
            case ".markdown": 
            case "markdn": return "text/plain";

            case ".m":
            case ".mm": return "text/x-c";

            case ".php":
            case ".php3":
            case ".php4":
            case ".php5":
            case ".phtml":
            case ".ph3":
            case ".ph4": 
            case ".ctp": return "text/plain";

            case ".pl":
            case ".pm":
            case ".t":
            case "p6":
            case "pl6":
            case "pm6":
            case "nqp":
            case ".pod": return "text/x-script.perl";
            


            case ".ps1":
            case ".psm1": 
            case ".psd1":
            case ".pssc":
            case ".psrc": return "text/plain";

            case ".py":
            case ".rpy":
            case ".pyw":
            case ".cpy":
            case ".gyp":
            case ".gypi": return "text/x-script.python";

            case ".r":
            case ".rhistory":
            case ".rprofile":
            case ".rt": return "text/plain";

            case ".cshtml": return "text/html";

            case ".rb":
            case ".rbx":
            case ".rjs":
            case ".gemspec":
            case ".rake":
            case ".ru":
            case ".erb":  return "text/plain";

            case ".rs": return "text/plain";

            case ".scala":
            case ".sc": return "text/plain";

            case ".scss": return "text/plain";

            case ".shadder": return "text/plain";

            case ".sh": 
            case ".bash": 
            case ".bashrc": 
            case ".bash_aliases": 
            case ".bash_profile": 
            case ".bash_login": 
            case ".ebuild": 
            case ".install": 
            case ".profile": 
            case ".bash_logout": 
            case ".zsh": 
            case ".zshrc": 
            case ".zprofile": 
            case ".zlogin": 
            case ".zlogout": 
            case ".zshenv": 
            case ".zsh-theme": return "text/plain";

            case ".sql":
            case ".dsql": return "text/plain";  

            case ".swift": return "text/plain";

            case ".ts": return "text/plain";  

            case ".tsx": return "text/plain";     

            case ".vb":
            case ".vba":
            case "brs":
            case ".bas":  
            case ".vbs": return "text/plain";

            case ".xml":
            case ".xsd":
            case ".ascx":
            case ".atom":
            case ".axml":
            case ".bpmn":
            case ".config":
            case ".cpt":
            case ".csl":
            case ".csproj":
            case ".csproj.user":
            case  ".dita":
            case ".ditamap":
            case ".dtd":
            case ".dtml":
            case ".fsproj":
            case ".fxml":
            case ".iml":
            case ".isml":
            case ".jmx":
            case ".launch":
            case ".menu":
            case ".mxml":
            case ".nuspec":
            case  ".opml":
            case ".owl":
            case ".proj":
            case ".props":
            case ".pt":
            case ".publishsettings":
            case ".pubxml":
            case ".pubxml.user":
            case ".rdf":
            case ".rng":
            case ".rss":
            case ".shproj":
            case ".storyboard":
            case ".svg":
            case ".targets":
            case ".tld":
            case ".tmx":
            case ".vbproj":
            case ".vbproj.user":
            case ".vcxproj":
            case ".vcxproj.filters":
            case ".wsdl":
            case ".wxi":
            case  ".wxl":
            case ".wxs":
            case ".xaml":
            case ".xbl":
            case ".xib":
            case ".xlf":
            case ".xliff":
            case ".xpdl":
            case ".xul":
            case ".xoml": return "text/xml";

            case ".yaml":
            case "eyaml":
            case "eyml":
            case ".yml": return "text/plain";
        }        
        return "text/plain";
    }

    /**
     * Get the language identifier for a file based on its extension(either VS Code or SARIF depending on param)
     * @param filePath the file path for the file being analyzed
     * @param sarifConvention (optional) if true, uses SARIF terminology where it deviates, false/default is VS Code identifiers
     */
    public getLangFromPath(filePath : string, sarifConvention : boolean = false) : string
    {
        let path = require('path');

        let extension : string = path.extname(filePath);
        switch (extension.toLowerCase())
        {
            case ".bat": return "bat";
            case ".cmd": return "cmd";

            case ".clj":
            case ".cljs":
            case ".cljc":
            case ".cljx":
            case ".clojure":
            case "edn": return "clojure";

            case ".coffee":
            case ".cson": return "coffeescript";

            case ".c": return "c";

            case ".cpp":
            case ".cc":
            case ".cxx":
            case ".c++":
            case ".h": 
            case ".hh":
            case ".hxx":
            case ".hpp":
            case ".h++": return (sarifConvention) ? "cplusplus" : "cpp";

            case ".cs":
            case ".csx":
            case ".cake": return "csharp";

            case ".css": return "css";

            case ".dockerfile": return "dockerfile";

            case ".fs":
            case ".fsi":
            case ".fsx":
            case ".fsscript": return "fsharp";

            case ".go": return "go";

            case ".groovy": 
            case ".gvy": 
            case ".gradle": return "groovy";

            case ".handlebars":
            case ".hbs": return "handlebars";

            case ".hlsl":
            case ".fx":
            case ".fxh":
            case ".vsh":
            case ".psh":
            case ".cfinc":
            case ".hlsli": return "hlsl";

            case ".html":
            case ".htm":
            case ".shtml":
            case ".xhtml":
            case ".mdoc":
            case ".jsp":
            case ".asp":
            case ".aspx":
            case ".jshtm":
            case ".volt":
            case ".ejs":
            case ".rhtml": return "html";

            case ".ini": return "ini";

            case ".jade": 
            case ".pug": return "jade";

            case ".java": 
            case ".jav": return "java";

            case ".jsx": return "javascriptreact";

            case ".js":
            case ".es6":
            case "mjs":
            case ".pac": return "javascript";

            case ".json":
            case ".bowerrc":
            case ".jshintrc":
            case ".jscsrc":
            case ".eslintrc":
            case ".babelrc":
            case ".webmanifest":
            case ".code-workspace": return "json";

            case ".less": return "less";

            case ".lua": return "lua";

            case ".mk": return "makefile";

            case ".md":
            case ".mdown":
            case ".markdown": 
            case "markdn": return "markdown";

            case ".m":
            case ".mm": return (sarifConvention) ? "objectivec" : "objective-c";
            
            case ".php":
            case ".php3":
            case ".php4":
            case ".php5":
            case ".phtml":
            case ".ph3":
            case ".ph4": 
            case ".ctp": return "php";

            case ".pl":
            case ".pm":
            case ".t":
            case "p6":
            case "pl6":
            case "pm6":
            case "nqp":
            case ".pod": return "perl";


            case ".ps1":
            case ".psm1": 
            case ".psd1":
            case ".pssc":
            case ".psrc": return "powershell";

            case ".py":
            case ".rpy":
            case ".pyw":
            case ".cpy":
            case ".gyp":
            case ".gypi": return "python";

            case ".r":
            case ".rhistory":
            case ".rprofile":
            case ".rt": return "r";

            case ".cshtml": return "razor";

            case ".rb":
            case ".rbx":
            case ".rjs":
            case ".gemspec":
            case ".rake":
            case ".ru":
            case ".erb":  return "ruby";

            case ".rs": return "rust";

            case ".scala":
            case ".sc": return "scala";

            case ".scss": return "scss";

            case ".shadder": return "shaderlab";

            case ".sh": 
            case ".bash": 
            case ".bashrc": 
            case ".bash_aliases": 
            case ".bash_profile": 
            case ".bash_login": 
            case ".ebuild": 
            case ".install": 
            case ".profile": 
            case ".bash_logout": 
            case ".zsh": 
            case ".zshrc": 
            case ".zprofile": 
            case ".zlogin": 
            case ".zlogout": 
            case ".zshenv": 
            case ".zsh-theme": return "shellscript";

            case ".sql":
            case ".dsql": return "sql";  

            case ".swift": return "swift";

            case ".ts": return "typescript";  

            case ".tsx": return "typescriptreact";     

            case ".vb":
            case ".vba":
            case "brs":
            case ".bas":  
            case ".vbs": return (sarifConvention) ? "visualbasic" : "vb";

            case ".xml":
            case ".xsd":
            case ".ascx":
            case ".atom":
            case ".axml":
            case ".bpmn":
            case ".config":
            case ".cpt":
            case ".csl":
            case ".csproj":
            case ".csproj.user":
            case  ".dita":
            case ".ditamap":
            case ".dtd":
            case ".dtml":
            case ".fsproj":
            case ".fxml":
            case ".iml":
            case ".isml":
            case ".jmx":
            case ".launch":
            case ".menu":
            case ".mxml":
            case ".nuspec":
            case  ".opml":
            case ".owl":
            case ".proj":
            case ".props":
            case ".pt":
            case ".publishsettings":
            case ".pubxml":
            case ".pubxml.user":
            case ".rdf":
            case ".rng":
            case ".rss":
            case ".shproj":
            case ".storyboard":
            case ".svg":
            case ".targets":
            case ".tld":
            case ".tmx":
            case ".vbproj":
            case ".vbproj.user":
            case ".vcxproj":
            case ".vcxproj.filters":
            case ".wsdl":
            case ".wxi":
            case  ".wxl":
            case ".wxs":
            case ".xaml":
            case ".xbl":
            case ".xib":
            case ".xlf":
            case ".xliff":
            case ".xpdl":
            case ".xul":
            case ".xoml": return "xml";

            case ".yaml":
            case "eyaml":
            case "eyml":
            case ".yml": return "yaml";
        }        
        return "plaintext";
    }
    
    /**
     * Create a URI from a file path
     * @param filePath the filepath a URI should be created from
     * @return a URI format for the file based on its provided path
     */
    public fileToURI(filePath : string) : string
    {
        if (typeof filePath !== 'string') {
            throw new Error('Expected a string');
        }
    
        let path = require('path');

        var pathName = path.resolve(filePath).replace(/\\/g, '/');
    
        // Windows drive letter must be prefixed with a slash
        if (pathName[0] !== '/') {
            pathName = '/' + pathName;
        }
    
        return encodeURI('file://' + pathName);
    };        
}