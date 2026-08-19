# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.108] - 2026-08-19
### Added
- Added Kubernetes Security Baseline rules (`DS200000`-`DS200007`) covering privileged containers, privilege escalation, host namespace sharing, writable root filesystems, running as root, unpinned images, dangerous Linux capabilities, and `hostPath` volumes. These are the first rules to use the engine's `ymlpaths` support, which no shipped rule had used.
- Added package source rules `DS205000` (a `nuget.config` `<packageSources>` with no `<clear />`, so the sources are added to those inherited from machine and user level configuration rather than replacing them) and `DS205001` (`--extra-index-url` and `PIP_EXTRA_INDEX_URL`).
- Added `DS114352`, which detects connection strings that leave transport encryption optional (`Encrypt=False`, `TrustServerCertificate=True`, `sslmode=prefer`/`allow`/`disable`, MySQL `SslMode=Preferred`/`None`, JDBC `useSSL=false`). Guidance for this rule was already in the repository but no rule referenced it.
- Added secret detection for PEM private key blocks (`DS173238`), provider access tokens from GitHub, AWS, Google, Slack, Stripe, npm, SendGrid and GitLab (`DS173239`), and Azure Storage account keys and shared access signatures (`DS173240`). The two existing secret rules keyed on 30 or more lowercase hex characters and matched none of these formats.
- Added `DS610000` for anchors using `target="_blank"` without `rel="noopener noreferrer"`. Its guidance was already present but the rule could not be written because no `html` language existed.
- Added deserialization rules for the libraries ADM.10010 names as unapproved: Python `torch.load`, `joblib.load`, `dill.load` and `marshal` (`DS425050`); PyYAML `yaml.load` without a safe loader (`DS425060`); .NET `BinaryFormatter`, `SoapFormatter`, `NetDataContractSerializer`, `LosFormatter` and `ObjectStateFormatter` (`DS425070`); `JavaScriptSerializer` and `SimpleTypeResolver` (`DS425080`); and Boost Property Tree (`DS425090`).
- Added XXE rules for .NET `DtdProcessing.Parse` and `ProhibitDtd = false` (`DS132782`), .NET `XmlResolver` assignment (`DS132783`), Java parser factories with no hardening feature anywhere in the file (`DS132784`), PHP `libxml_disable_entity_loader(false)` and `LIBXML_NOENT` (`DS132785`), and lxml entity resolution (`DS132786`). Previous coverage was Objective-C and Swift only.
- Added `DS154190` (the `IsBad*Ptr` family), `DS154191` (`CopyMemory` and `RtlCopyMemory`), `DS154192` (`strlen`, `wcslen`, `_tcslen`, `lstrlen`) and `DS154193` (Objective-C method swizzling).
- Added `html`, `dockerfile`, `terraform`, `bicep`, `kotlin`, `scala`, `dart`, `toml`, `gradle`, `msbuild` (`.props`/`.targets`), `dotenv`, `makefile` and `pem` language definitions with matching comment syntax. 32 rules declare no `applies_to` and so apply to every known language, meaning each addition extends those rules to a file type that was previously skipped.
- Added `file-names` entries so extensionless files are scanned. `Dockerfile`, `Makefile`, `Cargo.toml`, `id_rsa` and the shell dotfiles have no extension, so extension-based matching alone never reached them.
- Added `must-match` self-tests to the 32 rules that had none. Every rule now has a positive self-test.

### Fix
- Fixed the four rules that were never shipped. `android.json` (`DS180000`, `DS180001`, `DS180002`) and `xslt_scripting.json` (`DS132781`) were missing from the hand-maintained `<EmbeddedResource>` list in `Microsoft.DevSkim.csproj`, so they were absent from the default rule set. This is also why their dangling `rule_info` references never failed CI, since the tests iterate the shipped set. Their missing guidance has been written.
- Fixed `DS180000`, which bound the default XML namespace to the Maven POM namespace and matched `//default:application`. A real `AndroidManifest.xml` has no default namespace, so the rule could not fire on one; its self-test used a manifest with the Maven namespace and therefore passed while testing a document shape that does not occur.
- Fixed `DS132781`, which declared `applies_to: ["CSharp"]`. Language names are matched exactly against `languages.json`, which defines `csharp`, so the rule reported nothing.
- Fixed `DS191340`, which used `$1` as a backreference. .NET spells that `\1`, and `$` is an end-of-line anchor, so the pattern could never match.
- Fixed `DS440016`'s `--(sslv2|sslv3|tlsv1|tlsv11|tlsv1\.1|tlsv1\.2)` alternation, which was ordered shortest-first. .NET alternation is leftmost-first rather than longest-match, so `tlsv1` always won and the `tlsv11`, `tlsv1\.1` and `tlsv1\.2` branches were unreachable; `curl --tlsv1.1` reported a span covering only `--tlsv1`.
- Fixed `DS440016`'s `--secure-protocol=` pattern, which was typed as `string` and therefore word-boundary anchored. A leading hyphen is not a word character, so `wget --secure-protocol=SSLv3` was not reported at all. It is now a regex that also covers the protocol value following the flag.
- Repointed `DS140021` (`strlen`) from `DS154189` to `DS154192`. `strlen` moved into `DS154192` in this release, which left the old override inert and made `strlen(s)` report twice at the same severity.
- Gave the two unrelated rules that both used the ID `DS440011` distinct IDs. SARIF emits one `tool.driver.rules` entry per rule ID, so findings from the `hardcoded_tls.json` rule were reported with the other rule's name and a `helpUri` pointing at the wrong guidance document, and suppressing either ID suppressed both. The `hardcoded_tls.json` rule is now `DS440017`.
- Removed `DS440060`, which had an empty `patterns` array and so could not produce a finding while still occupying an entry in SARIF tool metadata.
- Normalised `DS450003`'s severity from `manualreview` to `ManualReview`.

### Changed
- Extended `DS154189` from 80 to 178 alternatives. Comparing the shipped rules against the 194 APIs enumerated in ADM.10082 found 113 that DevSkim did not report: 108 absent, and 5 present only in the wrong case, which `RegexWord` does not match.
- Moved `wcslen` and `_tcslen` from `DS154189` into `DS154192` so the same defect is not reported at two different severities depending on which variant is used.
- Added `javascriptreact` and `typescriptreact` to the 7 rules that target `javascript` or `typescript`. Both languages were already defined and mapped to `.jsx` and `.tsx`, but no rule named them, so React source received none of those rules.

### Dependencies
- Consolidated the open Dependabot pull requests (#765, #766, #767, #768, #769) into a single update for the VS Code extension: `linkify-it` 5.0.1 to 5.0.2, `fast-uri` 3.1.2 to 3.1.4, `undici` 7.24.6 to 7.29.0, and `brace-expansion` 1.1.14 to 1.1.16 and 5.0.5 to 5.0.8.
- Bumped `vscode-languageclient` from 7.0.0 to 10.1.0 in the extension client, which pulls `vscode-languageserver-protocol` up to 3.18.2 and replaces the transitive `minimatch` 3.1.5 chain with 10.2.5.
- Bumped `@types/vscode` from 1.77.0 to 1.91.0 to match the new minimum VS Code version.
- Bumped `typescript` from 4.9.5 to 5.9.3; the `vscode-languageclient` 10 type declarations use the `NoInfer` utility type, which requires TypeScript 5.4 or newer.
- Pinned `brace-expansion` to 1.1.16 and 5.0.8 and `minimatch` to 10.2.5 rather than the 1.1.18, 5.0.9, and 10.2.6 that Dependabot had selected. Those three releases have since been removed from the npm registry, so the packages could no longer be restored and the build failed with a 404 while fetching them.

### Changed
- Raised the minimum VS Code version supported by the extension from 1.63 to 1.91, which `vscode-languageclient` 10 requires.
- Migrated `client/extension.ts` to the `vscode-languageclient` 8+ client lifecycle: `LanguageClient.start()` returns a promise instead of a disposable and `onReady()` no longer exists, so notification handlers are now registered before the client starts and the client is disposed through the extension context.
- Switched the VS Code extension TypeScript projects to `node16` module resolution, which `vscode-languageclient` 10 requires because it declares its entry points only through `exports`.

## [1.0.95] - 2026-07-31
### Pipeline
- Added `.github/dependabot.yml` so Dependabot consolidates npm, NuGet, and GitHub Actions version updates into a single weekly pull request via a multi-ecosystem group, groups security updates per ecosystem into one pull request each, and labels its pull requests `no changelog` so the changelog gate passes.

### Documentation
- Corrected the changelog guidance in `.github/copilot-instructions.md` to derive the version heading from Nerdbank.GitVersioning (`nbgv get-version`) instead of incrementing the previous entry, which had let the changelog headings drift behind the versions actually built, and documented the `no changelog` label as the gate's escape hatch.

## [1.0.87] - 2026-07-15
### Pipeline
- Updated the CLI release pipeline to use the .NET 10 SDK when restoring, building, packaging, and releasing .NET 10 targets.

## [1.0.86] - 2026-06-10
### Added
- Added .NET 10 as a publishing target for the DevSkim library, CLI, and Language Server.

### Changed
- Swapped the VS Code extension language server to publish against .NET 10.
- Kept the DevSkim CLI dotnet tool portable (framework-dependent) by setting `CreateRidSpecificToolPackages=false`, avoiding the .NET 10 SDK's new RID-specific tool packaging that broke `dotnet pack` with NU5017 and would have dropped arm64 and pre-.NET 10 SDK install support.

### Pipeline
- Added .NET 10 (`10.0.x`) SDK to the CLI, VS Code, and Visual Studio build pipelines.

## [1.0.85] - 2026-06-10
### Dependencies
- Bump qs from 6.15.0 to 6.15.2 in /DevSkim-VSCode-Plugin (#753)

## [1.0.84] - 2026-06-10
### Dependencies
- Bump @azure/identity from 4.13.0 to 4.13.1 and remove the now-unused uuid transitive dependency in /DevSkim-VSCode-Plugin (#752)

## [1.0.83] - 2026-06-10
### Dependencies
- Bump tmp from 0.2.5 to 0.2.7 in /DevSkim-VSCode-Plugin (#754)

## [1.0.82] - 2026-05-12
### Dependencies
- Update dependencies for VS Code Extension

## [1.0.81] - 2026-04-16
### Pipeline
- Updates to fix release pipeline for VSCode extension.
 
## [1.0.80] - 2026-04-15
### Dependencies
- Run Npm Audit Fix to update VS Code Dependencies.


## [1.0.79] - 2026-04-14
### Changed
- Rewrote Visual Studio extension using VisualStudio.Extensibility SDK for VS2022/2026 compatibility
- Replaced legacy MEF-based ILanguageClient with LanguageServerProvider from new VS Extensibility SDK
- Implemented Windows Job Object for reliable language server process cleanup when VS exits
- Added settings management using VS Extensibility SDK settings API with localized string resources
- Enhanced code actions and fixes handling for better VS compatibility

### Pipeline
- Reintroduced build and release pipelines for the Visual Studio extension
- Added `Pipelines/vs/devskim-visualstudio-pr.yml` for PR validation builds
- Added `Pipelines/vs/devskim-visualstudio-release.yml` for release signing, VS Marketplace publishing, and GitHub releases

## [1.0.78] - 2026-03-30
### Dependencies
- Updated VS Code plugin npm dependencies to resolve security vulnerabilities (consolidates dependabot PRs #725, #727, #728, #729, #730, #731, #735):
  - Bumped `picomatch` from 2.3.1 to 2.3.2
  - Bumped `undici` to 7.24.6
  - Bumped `qs` to 6.15.0
  - Bumped `minimatch` to latest patched versions (3.1.5 / 10.2.4)
  - Bumped `flatted` from 3.3.2 to 3.4.2
  - Bumped `@vscode/test-electron` from 1.6.1 to 2.5.2 (removes vulnerable `@tootallnate/once`)
  - Additional transitive dependency updates via `npm audit fix`

## [1.0.77] - 2026-03-26
### Pipeline
Try to fix VS Code Publishing Pipeline issue with VSCE tool install

## [1.0.76] - 2026-02-12
### Fix
- Fixed DS126858 rule (Weak/Broken Hash Algorithm) false positive when MD5 is explicitly disabled via flags like `--nomd5`, `nomd5`, `no-md5`, `no_md5`, or `disable_md5_check`

## [1.0.75] - 2026-02-06
### Changed
- Removed unnecessary uninstall/reinstall of @vscode/vsce from postinstall script in VSCode plugin

## [1.0.74] - 2026-02-05
### Fix
- Fixed overly broad filename regex in .NET Framework configuration rules (DS450001, DS450002, DS450003) that incorrectly matched JSON files containing `.config` in their names (e.g., `file.test.config.json`), causing XML parsing errors

## [1.0.73] - 2026-02-04
### Fix
Suppress DS173237 on all-zero values

## [1.0.72] - 2026-02-04
### Added
- Added Copilot instructions file (.github/copilot-instructions.md) with repository-specific guidance for AI coding agents
- Documented build, test, and development workflows for C# and TypeScript components
- Included special instructions for handling nuget.config and .npmrc files
- Added mandatory Changelog.md update requirements for all PRs

## [1.0.71] - 2026-02-03
### Fix
- Fixed invalid JSON in package.json (trailing comma in scripts section) that caused npm parse errors in Azure DevOps pipeline
- Fixed @vscode/vsce package installation issue where `out/` folder was missing, causing "Cannot find module './out/main'" error

### Dependencies
- Updated @vscode/vsce from 3.4.2 to 3.7.1

## [1.0.70] - 2026-01-29
### Pipeline
Fix release pipeline for VSCode extension

## [1.0.69] - 2026-01-28
### Pipeline
Move PR and release pipelines to new ADO organization

## [1.0.68] - 2025-12-04
### Dependencies
Bump jws from 3.2.2 to 3.2.3 in /DevSkim-VSCode-Plugin

## [1.0.67] - 2025-11-18
### Dependencies
Update Dependencies for VS Code Extension

## [1.0.66] - 2025-09-04
### Dependencies
Update Dependencies for C# Projects

## [1.0.65] - 2025-09-04
### Dependencies
Update VS Code IDE `tmp` Dependency

## [1.0.64] - 2025-07-31
### Pipeline
- Updated VS Code extension pipeline scripts to properly configure .npmrc files before credential provider setup
- Modified updatePackageLock.js script to accept registry base URL as command line argument instead of hardcoded value
- Added support for different registry configurations for PR builds vs release builds

## [1.0.63] - 2025-07-30
### Pipeline
Fix for VS Code Pipeline Build

## [1.0.63] - 2025-07-29
### Fix
Fixes Sarif Markdown value failing to populate the rule provided recommendation value. #697

### Dependencies
Update dependencies 

### Tests
Adds test cases for SarifWriter

## [1.0.62] - 2025-07-23
### Pipelines
Pipeline updates

## [1.0.61] - 2025-07-11
### Pipelines
Pipeline updates

## [1.0.60] - 2025-06-26
### Pipelines
Pipeline updates

## [1.0.59] - 2025-06-10
### Misc (non-code)
Removes old doc publish workflow. 

## [1.0.58] - 2025-06-09
### New Feature
Adds a `vs` output format to leverage the DevSkim CLI as a build task in a csproj.

## [1.0.57] - 2025-05-28
### Fix
Fix an issue handling non-ascii paths when launching LSP in VS Code Extension

### Dependencies
Update Dependencies

## [1.0.56] - 2025-04-14
### Tests
Migrate to MTP

## [1.0.55] - 2025-04-14
### Dependencies
Updates Dependencies

## [1.0.54] - 2025-04-09
### Dependencies
Updates Dependencies

## [1.0.53] - 2025-04-01
### Documentation
Adds a link to the Microsoft Privacy Statement to the Readme.

## [1.0.52] - 2024-12-09
### Dependencies
Updates Dependencies

### .Net Targets
CLI now targets .NET 8.0 and .NET 9.0, .NET 6.0/7.0 targeting removed. DevSkim Library component retains .Net Standard 2.1 support.

## [1.0.51] - 2024-12-09
### Fix
Fix confidence filtering at rule level.

## [1.0.50] - 2024-12-05
### Fix
Fixes #664 handling of options from IgnoreRuleMap when using OptionsJson

### New Functionality
Adds `include-globs` argument to require all scanned files match a specific glob pattern #663.

## [1.0.49] - 2024-12-03
### Rules
Fixed false positives and false negatives in outdated/banned SSL/TLS protocols. #649

## [1.0.48] - 2024-11-20
### Dependencies
Update VS Code Extension Dependencies

## [1.0.47] - 2024-11-12
### Pipeline
Pipeline only changes

## [1.0.46] - 2024-11-05
### Pipeline
Pipeline only changes

## [1.0.45] - 2024-11-01
### Pipeline
Pipeline only changes

## [1.0.44] - 2024-11-01
### Pipeline
Pipeline only changes

## [1.0.43] - 2024-10-29
### Pipeline
Pipeline only changes

## [1.0.42] - 2024-08-26
## Fix
Fixes suppression command to not perturb line breaks, particularly when a file has findings which are not selected for suppression. #631

## [1.0.41] - 2024-08-23
## Rules
Extend the false positive fix for the issue reported in #548 to Sdk-style msbuild projects.

## [1.0.40] - 2024-7-08
## Fix
Fixes extraneous printing of git errors when git ignore checking is enabled during analysis.

## [1.0.39] - 2024-6-26
### Pipelines
Pipeline maintenance.

## [1.0.38] - 2024-6-05
### Incomplete Guidance
Expanded content for rule guidance containing "TO DO"s.

## [1.0.37] - 2024-5-23
### Missing Guidance
Added guidance for several rules such as weak hash algorithm, disabling certificate validation, and TLS client configuration.

## [1.0.36] - 2024-05-21
### Rules
Fix substitution pattern in PHP Request rule.

## [1.0.35] - 2024-5-8
### Pipeline
Pipeline only changes

## [1.0.34] - 2024-3-18
### Pipeline
Pipeline only changes

## [1.0.33] - 2024-3-13
### Fix
Fixes properly setting the default value for the `OutputFileFormat` and `OutputTextFormat` fields when using the `options-json` argument to the analyze command.

## [1.0.32] - 2024-3-04
### Pipeline
Improvement to pipeline to allow rerunning failed deploy jobs.

## [1.0.31] - 2024-2-28
### Sarif Format
Populate additional fields for GitHub Code scanning

### Rules
Populate Confidence values for rules

### Dependencies
Update Dependencies

### Engine
Prioritize confidence value from Pattern level in Issue records but fall back to rule level if not specified.

## [1.0.30] - 2024-1-31
### Pipeline
Additional pipeline fixes

## [1.0.29] - 2024-1-17
### Pipeline
Fix GitHub binary release process

### Dependencies
Update Application Inspector dependency

## [1.0.28] - 2024-1-4
### Fix
Remove trailing period after general guidance URI in output to make it clickable when automatically converted to uri by terminal

### Dependencies
Update dependencies

## [1.0.27] - 2023-12-12
### Pipelines
Move GitHub Release task to last task in publish pipeline.

## [1.0.26] - 2023-12-05
### Dependencies
Update dependencies.

### Framework
Build using .NET 8

## [1.0.25] - 2023-11-10
### Dependencies
Update dependencies. Resolves an issue with some xpath queries via AppInspector Rules engine https://github.com/microsoft/ApplicationInspector/pull/567

## [1.0.24] - 2023-10-10
### Dependencies
Update OmniSharp language server and App Inspector dependencies.

## [1.0.23] - 2023-10-05
### Miscellaneous
Update deployment pipeline version

## [1.0.22] - 2023-09-14
### Dependencies
Update dependencies - incorporate a fix for an issue with JSONPath selection used for matching boolean values. https://github.com/microsoft/ApplicationInspector/pull/561

### Rules
Fix a JSON formatting error in the android debuggable rule

### Miscellaneous
Delete advisory parser script. #586

## [1.0.21] - 2023-09-11
### Dependencies
Update action versions for github workflows.

## [1.0.20] - 2023-08-28
### Fixes
Removes workaround for 404 sarif schema uri

### Dependencies
Updates dependencies to latest.

### VS Extension
Fix ordering of proposed fixes in UX. #582

## [1.0.19] - 2023-08-22
### VS Extension
Fix concurrent access issue with cache storage for fixes. Fix #480

## [1.0.18] - 2023-08-09
### Rules
Fix language filtering on random number generator rules. Fix #468

## [1.0.17] - 2023-08-07
### Rules
Improve HTTP url detection rule to exclude more schema definitions.

## [1.0.16] - 2023-08-04
### Fixes
Fixes an issue with loading settings in the Visual Studio extension.

## [1.0.15] - 2023-07-31
### Rules
Fix false positives reported in #344, #548 and #549.

## [1.0.14] - 2023-07-27
### Fixes
Fixes an issue handling IEnumerable arguments specified with the options-json argument to Analyze.

### Dependencies
Updates RuleEngine dependency to fix an issue with handling matching strings with `//` in languages that use `//` for inline comment format.

## [1.0.13] - 2023-07-24
### Dependencies
Update VS Code Extension Dependencies

## [1.0.12] - 2023-07-24
### Guidance
Updated Guidance for DS126858 

## [1.0.11] - 2023-06-26
### Update Dependency
- Update SemVer dependency in VS Code Extension.

## [1.0.10] - 2023-06-26
### Fixed
- Removed sub scan workspace command in VS Code extension.

## [1.0.9] - 2023-06-26
### Fixed
- Fixed an issue in the VS Code Extension that would try to run the language server with dotnet on the system path instead of the version fetched by the .NET Install Tool extension.

## [1.0.8] - 2023-06-09
### Rules
- Adds new rules and improves precision of some existing rules.

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

