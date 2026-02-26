# Copilot Instructions for DevSkim

## Repository Overview

DevSkim is a framework of IDE extensions and language analyzers that provide inline security analysis in the dev environment as the developer writes code. The repository contains:

- **DevSkim Library** (C#/.NET): Core security analysis engine (`./DevSkim-DotNet/`)
- **DevSkim CLI** (C#/.NET): Command-line tool (`./DevSkim-DotNet/Microsoft.DevSkim.CLI/`)
- **Visual Studio Extension** (C#/.NET): VS extension (`./DevSkim-DotNet/Microsoft.DevSkim.VisualStudio/`)
- **VS Code Plugin** (TypeScript): VSCode extension (`./DevSkim-VSCode-Plugin/`)
- **Security Rules**: Default rules and guidance (`./rules/default/`, `./guidance/`)

## Critical Repository-Specific Rules

### Package Management Configuration

**⚠️ IMPORTANT**: This repository uses private Azure DevOps feeds for package management:

- **nuget.config**: Contains private feed configuration (`PublicRegistriesFeed`)
- **.npmrc files**: VSCode plugin uses `.npmrc.pipeline` for private feeds

**Rules for agents**:
1. You MAY temporarily modify `nuget.config` or `.npmrc` files to use public feeds (nuget.org, npmjs.com) when working locally
2. You MUST NOT commit these changes - always revert them before committing
3. Use `git restore nuget.config` or `git restore DevSkim-VSCode-Plugin/.npmrc.pipeline` before creating commits
4. The private feed configuration must remain in the repository commits

### Changelog Requirements

**⚠️ MANDATORY**: All pull requests MUST include an update to `Changelog.md`

- This project uses **squash merges**
- PR gate checks verify `Changelog.md` is updated
- Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
- Use semantic versioning: `[MAJOR.MINOR.PATCH]`

**When making changes**:
1. Add a new entry at the top of `Changelog.md` (after the header)
2. Use the **next patch version** (increment last number by 1)
3. Use today's date in YYYY-MM-DD format
4. Group changes by type: `### Fix`, `### Added`, `### Changed`, `### Dependencies`, `### Pipeline`, etc.
5. Write clear, actionable descriptions

**Example**:
```markdown
## [1.0.72] - 2026-02-04
### Added
- Added Copilot instructions for repository-specific guidance

### Changed
- Updated build documentation
```

## Building and Testing

### VS Code Plugin (TypeScript)

**Location**: `./DevSkim-VSCode-Plugin/`

**Setup**:
```bash
cd DevSkim-VSCode-Plugin
npm run setup          # Install dependencies and build .NET language server
npm run setup:release  # Release build
```

**Build**:
```bash
npm run compile        # Compile TypeScript
npm run build          # Full build (setup + compile)
npm run watch          # Watch mode for development
```

**Lint**:
```bash
npm run lint           # Run ESLint on TypeScript files
```

**Package**:
```bash
npm run pack-ext       # Package extension for release
npm run pack-ext:debug # Package extension for debug
```

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

### Language Server

**Build**:
```bash
cd DevSkim-DotNet/Microsoft.DevSkim.LanguageServer
dotnet publish -c Debug -f net8.0 -o ../../DevSkim-VSCode-Plugin/devskimBinaries
dotnet publish -c Release -f net8.0 -o ../../DevSkim-VSCode-Plugin/devskimBinaries
```

## Code Style and Conventions

### C# Code
- Follow standard C# naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Security-focused: prioritize secure defaults

### TypeScript Code
- Use ESLint configuration in `DevSkim-VSCode-Plugin/.eslintrc.js`
- Follow TypeScript best practices
- Use type annotations
- Avoid `any` types when possible

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
- For npm: Use `npm install` or edit `package.json`
- Document in Changelog.md under `### Dependencies`

### Debugging
- **VS Code plugin**: Use F5 in VS Code to launch Extension Development Host
- **.NET projects**: Use Visual Studio or `dotnet run`
- **Language Server**: Attach debugger to running process

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
