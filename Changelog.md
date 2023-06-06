# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.7] - 2023-06-06
### Fixed
- Fixes issue where the CLI global tool package was attempting to run with a mismatched runtime.

## [1.0.6] - 2023-05-25
### Republish
- Republish of 1.0.5 due to a release pipeline error

## [1.0.5] - 2023-05-25
### Added
- Add CHANGELOG.md

### Fixed
- Support ignore-case `i` and multi-line `m` modifiers on the Pattern property of Fixes.

## [1.0.4] - 2023-05-24
### Fixed
- Fixes output sarif returning not applicable fixes

## [1.0.3] - 2023-05-24
### Fixed
- Fixes output sarif for runs with rules with empty string for Recommendation and Description

## [1.0.2] - 2023-05-24
### Fixed
- Fix output sarif for runs with rules with null string for Recommendation and Description

## [1.0.1] - 2023-05-24
This version is a major refactor of DevSkim.

### Added
- Added fix and suppress commands that operate on the output sarif from Analyze and the source code scanned with analyze to apply fixes/suppressions

Usage: 
```bash
devskim analyze -I path/to/source -O myresults.sarif​
devskim fix -I path/to/source -O myresults.sarif --dry-run --all​
devskim suppress -I path/to/source -O myresults.sarif --dry-run --all
```
- Support jsonpath/xpath and ymlpath based rules
- New `--options-json` argument to analyze to specify DevSkim configuration via a JSON file, including ability to Ignore rules only for specific languages
- IDE extensions are now based on a unified C# Language Server, should have better performance and reliability and support new options like user provided Rules/Languages.
- DevSkim Rule format is now an extension of Application Inspector rule format

### Changed
- Input/output files are now named parameters (-I/--source-code and -O/--output-file), not positional parameters

Old: `devskim analyze path/to/src path/to/output.sarif -f sarif`

New: `devskim analyze -I path/to/src -O path/to/out.sarif`
- Sarif is now the default output format for the CLI
- DevSkim targets .NET 6.0 and .NET 7.0
- Rule self tests are now included directly in rule specification (must-match and must-not-match fields) and are checked by the Verify command.
- Visual Studio Extension now targets VS 2022 instead of VS 2019.
- VS Code Extension now requires VSC Engine 1.63 or later

### Removed
- Json is no longer supported as an output format argument to CLI
- Pack, test and catalogue commands removed from CLI

### Fixes
- Rule improvements and DevSkim engine performance and reliablity improvements.