using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using MediatR;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;


namespace DevSkim.LanguageServer;

internal class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private readonly ILogger<TextDocumentSyncHandler> _logger;
    private readonly ILanguageServerConfiguration _configuration;
    private readonly ILanguageServerFacade _facade;
    private readonly DocumentSelector _documentSelector = DocumentSelector.ForLanguage(new[] {"csharp"});
    private DevSkimRuleProcessor _processor;

    public TextDocumentSyncHandler(ILogger<TextDocumentSyncHandler> logger, ILanguageServerConfiguration configuration, ILanguageServerFacade facade)
    {
        _facade = facade;
        _logger = logger;
        _configuration = configuration;

        _processor = new DevSkimRuleProcessor(StaticScannerSettings.RuleSet, StaticScannerSettings.RuleProcessorOptions);
        _processor.EnableSuppressions = true;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;


    private async Task<Unit> GenerateDiagnosticsForTextDocument(string text, int? version, DocumentUri uri)
    {
        if (text == null)
        {
            _logger.LogDebug("\tNo content found");
            return Unit.Value;
        }

        var filename = uri.Path;
        // Diagnostics are sent a document at a time
        _logger.LogDebug($"\tProcessing document: {filename}");
        var issues = _processor.Analyze(text, filename).ToList();
        var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();
        var codeFixes = ImmutableArray<CodeFixMapping>.Empty.ToBuilder();
        _logger.LogDebug($"\tAdding {issues.Count} issues to diagnostics");
        foreach (var issue in issues)
        {
            if (!issue.IsSuppressionInfo)
            {
                var diag = new Diagnostic()
                {
                    Code = $"MS-CST-E.vscode-devskim: {issue.Rule.Id}",
                    Severity = DiagnosticSeverity.Error,
                    Message = $"{issue.Rule.Description ?? string.Empty}",
                    Range = new Range(issue.StartLocation.Line - 1, issue.StartLocation.Column, issue.EndLocation.Line - 1, issue.EndLocation.Column),
                    Source = $"DevSkim Language Server: [{issue.Rule.Id}]"
                };
                diagnostics.Add(diag);
                for (int i = 0; i < issue.Rule.Fixes?.Count; i++)
                {
                    CodeFix fix = issue.Rule.Fixes[i];
                    if (fix.Replacement is { })
                    {
                        codeFixes.Add(new CodeFixMapping(diag, fix.Replacement, uri.ToString()));
                    }
                }
            }
        }

        _logger.LogDebug("\tPublishing diagnostics...");
        _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
        {
            Diagnostics = new Container<Diagnostic>(diagnostics.ToArray()),
            Uri = uri,
            Version = version
        });
        foreach (var codeFixMapping in codeFixes.ToArray())
        {
            _facade.TextDocument.SendNotification("devskim/codefixmapping", codeFixMapping);
        }

        return Unit.Value;
    }

    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidChangeTextDocumentParams");
        if (StaticScannerSettings.ScanOnChange)
        {
            var content = request.ContentChanges.FirstOrDefault();
            if (content is null)
            {
                _logger.LogDebug("\tNo content found");
                return Unit.Value;
            }
            return await GenerateDiagnosticsForTextDocument(content.Text, request.TextDocument.Version, request.TextDocument.Uri);
        }
        return Unit.Value;            
    }

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidOpenTextDocumentParams");
        if (StaticScannerSettings.ScanOnOpen)
        {
            await Task.Yield();
            var content = request.TextDocument;
            return await GenerateDiagnosticsForTextDocument(content.Text, content.Version, request.TextDocument.Uri);
        }
        return Unit.Value;        
    }
    
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidCloseTextDocumentParams");
        if (_configuration.TryGetScopedConfiguration(request.TextDocument.Uri, out var disposable))
        {
            disposable.Dispose();
        }
        // TODO: Possibly need to clear diagnostics here based on the settings to clear issues on close. However, the request doesn't contain a "version" for the document, so not clear how to populate the version number for the published empty diagnostics

        return Unit.Task;
    }
    
    public override async Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidSaveTextDocumentParams");
        if (StaticScannerSettings.ScanOnSave)
        {
            // This type of request doesn't contain the file contents, unclear how to scan based on this request
        }
        return Unit.Value;
    }
    
    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentSyncRegistrationOptions() {
        DocumentSelector = _documentSelector,
        Change = Change,
        Save = new SaveOptions() { IncludeText = false }
    };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        // TODO: This should return the correct language based on the uri
        return new TextDocumentAttributes(uri, "csharp");
    }
}