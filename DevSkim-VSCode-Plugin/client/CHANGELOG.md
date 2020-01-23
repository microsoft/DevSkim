# Changelog

## Version 0.2.1
* There is now an experimental "Scan All" command that, when invoked, will scan every applicable file in the root workspace.  There are a couple of caveats due to limitations with VS Code - other extensions will also generate "problems" (everyone gets the message that the files were opened for analysis at the moment in VSC), and there is a maximum number of problems that the problems pane can display, so large code bases potentially won't see every issue.  When VSC addresses the first issue (its in their backlog) the latter issue should be less likley. 

## Version 0.2.0
* Added ability to apply rules to specific files (e.g. package.json or project.json, instead of all .json files)
* New manual review rules for deserialization in different languages (e.g. pickle.load in Python, or ReadObject in Java).  Manual review rules are not enabled by default and must be turned on via settings.  These are *potentially* severe security issues, but as DevSkim does not have strong data flow capabilities these rules will also flag safe uses.  
* Made the HTTP check a little smarter.  It now excludes various xmlns scenarios.  If there are other scenarios that should be whitelisted please tell us in on our github issues page and we will adjust the rules
* PHP rules to flag use of $_REQUEST and echo-ing $_GET/PUT/COOKIES without encoding
* DevSkim will no longer look for issues in the original file when looking at a git comparison of two files (it will still show issues in the current file on the file system in the git comparison view however)
* Updated to support the new rules schema (documentation at https://github.com/Microsoft/DevSkim/wiki).  This includes:
    * Ability to scope rules to specific parts of a source file (e.g. just executable source, just code comments, just HTML blocks [not yet implemented], or a combinations of those)
    * Chained patterns, allowing for more complex scenarios (i.e. If this pattern AND this patthern AND NOT this pattern, then its an issue).  see <extension directory>\rules\frameworks\php.json for an example of this
* Added facilities to make rule authoring a little easier:
    * There is now an action to reload rules (F1 > Devskim: Reload Rules) which is useful when writing and testing rules
    * Under settings there is the option to turn on rules validation.  If validateRulesFiles is set to true in the settings file each time DevSkim loads the rules it will verify they are in the correct format.  Any errors will be written to <extension directory>\rulesValidationLog.json.  If DevSkim can autocorrect the errors (for example, it detects a rule in the format of an old schema) it will write corrected rules to <extension directory>\newrules\
* There is now a setting to provide a base URL for more guidance.  By default it points to (currently stubbed out) guidance files in the devskim github repo, but an organization can download those files, modify them to be specific to their own organization, and point DevSkim at their own URL via that setting
* New setting (devskim.removeFindingsOnClose)to clear findings when a file is closed.  By default, findings will continue to be visible on the "Problems" pane in VS Code after a file has closed, but when this setting is turned to true Findings will only be listed for open files
* Viewing a comparison between a changed file and the original in the "Source Control" view will not flag issues in the original, just the changed file



## Version 0.1.4
* Fixed analysis exception that would trigger on certain workspace changes

## Version 0.1.3
* Added setting devskim.ignoreFiles to exclude specific files and directories from analysis.  The defaults are to ignore "out/\*","bin/\*", "node_modules/\*", ".vscode/\*","yarn.lock", "logs/\*", "\*.log" however the defaults can be overridden in the usersettings.  If there are other files that you think should be in the defaults please either open an issue on our github repo or edit [package.json](https://github.com/Microsoft/DevSkim-VSCode-Plugin/blob/master/client/package.json) and submit a PR  
* Added setting devskim.ignoreRulesList to allow the user to disable the processing of certain rules.  If a rule is being disabled because it is incorrectly flagging problems, please also open an issue on our Github repo so we can try and tune the rule better.  If you ran into problems other people are likely also hitting issues
* new rule to detect plaintext HTTP usage and prompt for HTTPS

## Version 0.1.2
* No longer reports findings that are commented out in source code
* A couple new rules for banned C API, SQL, PHP
* Improved multi-line findings detection/suppressions
* Reduced the number of "Severities" - "Low", "Informational", and "Defense in Depth" are now all "Best Practice"
* "Best Practice" is not enabled by default, but can be turned on with in the DevSkim settings.  

## Version 0.1.1
* Fixed bugs relating to scenarios where multiple security issues reside on a single line
* Added manual code review rules for eval usage in dynamic languages.  These will only be enabled if the manual review rules are turned on in the settings
* Improved rule detection for scenarios where a space occurs between a banned API and ()  (e.g. gets (str) instead of gets(str))
* Some spelling fixes