# Copilot Instructions for DevSkim

## Repository Overview

DevSkim is a .NET library and cross-platform command line tool that performs static security analysis of source code. The repository contains:

- **DevSkim Library** (C#/.NET): Core security analysis engine (`./DevSkim-DotNet/Microsoft.DevSkim/`)
- **DevSkim CLI** (C#/.NET): Command-line tool (`./DevSkim-DotNet/Microsoft.DevSkim.CLI/`)
- **Tests** (C#/.NET): MSTest project (`./DevSkim-DotNet/Microsoft.DevSkim.Tests/`)
- **Security Rules**: Default rules and guidance (`./rules/default/`, `./guidance/`)

## Critical Repository-Specific Rules

### Package Management Configuration

**⚠️ IMPORTANT**: This repository uses private Azure DevOps feeds for package management:

- **nuget.config**: Contains private feed configuration (`PublicRegistriesFeed`)

**Rules for agents**:
1. You MAY temporarily modify `nuget.config` to use public feeds (nuget.org) when working locally
2. You MUST NOT commit these changes - always revert them before committing
3. Use `git restore nuget.config` before creating commits
4. The private feed configuration must remain in the repository commits

### Changelog Requirements

**⚠️ MANDATORY**: All pull requests MUST include an update to `Changelog.md`

- This project uses **squash merges**
- PR gate checks verify `Changelog.md` is updated
- Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
- Version headings are `[MAJOR.MINOR.PATCH]` and must match the version the build actually produces

**Versioning**: this repo versions with [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning). `version.json` pins `MAJOR.MINOR` (currently `1.0`) and the patch number is the **git height**, which is the commit count since `version.json` last changed. Do **not** guess it by incrementing the previous changelog entry: an open PR's height shifts every time another PR merges ahead of it, and blindly incrementing bakes that drift in permanently.

**When making changes**:
1. Add a new entry at the top of `Changelog.md` (after the header)
2. Determine the version by asking Nerdbank.GitVersioning what this commit produces, rather than incrementing the previous entry:
   ```bash
   dotnet tool install --global nbgv   # once
   nbgv get-version -v SimpleVersion   # e.g. 1.0.95
   ```
   Run this **after** committing your change and with your branch rebased on the latest `main`, since the height includes your own commit. If your PR sits open while other PRs merge, re-run it and update the heading before merging.
3. Use today's date in YYYY-MM-DD format
4. Group changes by type: `### Fix`, `### Added`, `### Changed`, `### Dependencies`, `### Pipeline`, etc.
5. Write clear, actionable descriptions

**Example** (assuming `nbgv get-version -v SimpleVersion` reported `1.0.72`):
```markdown
## [1.0.72] - 2026-02-04
### Added
- Added Copilot instructions for repository-specific guidance

### Changed
- Updated build documentation
```

**Exception**: the gate ([`tarides/changelog-check-action`](https://github.com/tarides/changelog-check-action)) is skipped on PRs carrying the `no changelog` label. That is reserved for changes that are not user-visible, and Dependabot applies it automatically via `.github/dependabot.yml`. Do not use it to avoid writing an entry for a real change.

## Building and Testing

### .NET Projects (C#)

**Location**: `./DevSkim-DotNet/`

**Build**:
```bash
cd DevSkim-DotNet
dotnet build Microsoft.DevSkim.sln
dotnet build -c Release Microsoft.DevSkim.sln
```

**Test**:
```bash
cd DevSkim-DotNet
dotnet test Microsoft.DevSkim.Tests/Microsoft.DevSkim.Tests.csproj
```

**Run CLI**:
```bash
cd DevSkim-DotNet/Microsoft.DevSkim.CLI
dotnet run -- analyze --source-code <path>
```

## Code Style and Conventions

### C# Code
- Follow standard C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Security-focused: prioritize secure defaults

### Security Rules
- Rules are JSON files in `./rules/default/`
- Each rule has corresponding guidance in `./guidance/`
- Follow existing rule patterns when adding new rules

## Common Tasks

### Adding a New Security Rule
1. Create JSON rule in `./rules/default/`
2. Create guidance markdown in `./guidance/` with rule ID
3. Test rule with DevSkim CLI
4. Update tests if applicable
5. Update Changelog.md

### Updating Dependencies
- For .NET: Use `dotnet add package` or edit `.csproj` files
- Document in Changelog.md under `### Dependencies`

### Debugging
- Use Visual Studio, VS Code with the C# extension, or `dotnet run`

## Git Workflow

1. Changes are made on feature branches
2. PRs target the `main` branch
3. PRs require:
   - Changelog.md update
   - Passing CI/CD checks
   - Code review approval
4. Merges use **squash merge** strategy

## Additional Resources

- [Build from Source](https://github.com/microsoft/DevSkim/wiki/Build-from-Source)
- [Writing Rules](https://github.com/Microsoft/DevSkim/wiki/Writing-Rules)
- [How to Contribute](https://github.com/Microsoft/DevSkim/wiki/How-to-Contribute)
- [Command Line Interface](https://github.com/microsoft/DevSkim/wiki/Command-Line-Interface)
