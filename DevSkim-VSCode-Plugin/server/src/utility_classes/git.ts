/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { PathOperations } from './pathOperations';
import { DirectoryInfo } from '../devskimObjects';

/**
 * Class to extract git repo info from folders
 */
export class gitHelper
{
    constructor()
    {

    }

    /**
     * Recursively crawls a directory structure, building a DirectoryInfo[] object that is passed to the callback
     * The first value in the array will be the starting directory EVEN if it doesn't have git info - if it does not
     * the various values will either be empty or NULL.  All subsequent array elements will be directories that have
     * git info
     * 
     * @param startDirectory the directory to start crawling from
     * @param callback a function that is passed a DirectoryInfo[] created from calling the structure
     */
    public async getRecursiveGitInfo(startDirectory : string, callback : Function)
    {        
        let pathOp = new PathOperations();
        
        if(startDirectory.length == 0)
        {
            console.log("Error: no directory to analyze");
            return;
        }

        //clean up the directory path
        let directory: string = pathOp.normalizeDirectoryPaths(startDirectory);               
    
    
        //iterate through the sub folders
        let dir = require('node-dir'); 
        dir.subdirs(directory, (err,subdir) => {
            if (err)
            {
                console.log(err);
                throw err;
            }

            var getRepoInfo = require('git-repo-info');
    
            //start with an object for the parent directory, even if it has no git info
            let directories : DirectoryInfo[] = [];
            let baseDir : DirectoryInfo = Object.create(null);
            baseDir.directoryPath = directory;
            baseDir.gitInfo = getRepoInfo(directory);
            baseDir.gitRepo = this.getRepo(directory);
            directories.push(baseDir);
             
            for(let dir of subdir)
            {
                dir = pathOp.normalizeDirectoryPaths(dir);
                
                if(dir.substr(dir.length-4) == ".git" && dir.substr(0,dir.length-5) != directory)
                {          
                    directories.push(this.getGitDirectoryInfo(dir));
                }
            }
            //now pass to the callback the full directory structure
            callback(directories); 
        });  
    }

    /**
     * Create a DirectoryInfo object from the contents of a .git folder
     * the directoryPath property will be the parent directory of the .git
     * folder
     * @param gitDirectory a .git folder path
     * @return a DirectoryInfo object populated with the git info
     */
    public getGitDirectoryInfo(gitDirectory: string) : DirectoryInfo
    {
        var getRepoInfo = require('git-repo-info');
        let curDir : DirectoryInfo = Object.create(null);

        //the directory path is the parent of whichever directory contains a .git folder
        curDir.directoryPath = gitDirectory.substr(0,gitDirectory.length-4);

        curDir.gitInfo = getRepoInfo(gitDirectory);
        curDir.gitRepo = this.getRepo(gitDirectory);
        return curDir;      
    }

    /**
     * Finds the repo url from the config file located in the git directory
     * @param gitDirectory path to a .git folder
     * @return the url for the repository, or empty string if there is none
     */
    public getRepo(gitDirectory: string) : string
    {
        //Do some basic error checking to begin with - empty path, not a path to .git
        //no config file, etc.
        if(gitDirectory.length == 0)
        {
            return "";
        }
        const path = require('path');
    
        if(gitDirectory.substr(gitDirectory.length-4) != ".git" )
        {        
            gitDirectory = path.join(gitDirectory,".git");
        }
    
        gitDirectory = path.join(gitDirectory,"config");
        const fs = require('fs');
        if(fs.existsSync(gitDirectory))
        {
            //now that we have a real .git config file, read it and parse for the URL
            let config : string = fs.readFileSync(gitDirectory, "utf8");
            let urlRegex: RegExp = /url\s*=\s*(.*)\s*/;
            let XRegExp = require('xregexp');
            let match = XRegExp.exec(config,urlRegex);
            if(match)
            {
                return match[1];
            }        
        }
        
        return "";
    }    
}