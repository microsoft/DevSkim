using System.Collections.Immutable;
using MediatR;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;


namespace DevSkim.LanguageServer;

internal class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private readonly ILogger<TextDocumentSyncHandler> _logger;
    private readonly ILanguageServerFacade _facade;
    private readonly DocumentSelector _documentSelector = DocumentSelector.ForLanguage(StaticScannerSettings.RuleProcessorOptions.Languages.GetNames());
    private DevSkimRuleProcessor _processor => StaticScannerSettings.Processor;

    public TextDocumentSyncHandler(ILogger<TextDocumentSyncHandler> logger, ILanguageServerFacade facade)
    {
        _facade = facade;
        _logger = logger;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    private async Task<Unit> GenerateDiagnosticsForTextDocument(string text, int? version, DocumentUri uri)
    {
        if (text == null)
        {
            _logger.LogDebug("\tNo content found");
            return Unit.Value;
        }

        string filename = uri.Path;
        if (StaticScannerSettings.IgnoreFiles.Any(x => x.IsMatch(filename)))
        {
            _logger.LogDebug($"\t{filename} was excluded due to matching IgnoreFiles setting");
            return Unit.Value;
        }
        // Diagnostics are sent a document at a time
        _logger.LogDebug($"\tProcessing document: {filename}");
        List<Issue> issues = _processor.Analyze(text, filename).ToList();
        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();
        ImmutableArray<CodeFixMapping>.Builder codeFixes = ImmutableArray<CodeFixMapping>.Empty.ToBuilder();
        _logger.LogDebug($"\tAdding {issues.Count} issues to diagnostics");
        foreach (Issue issue in issues)
        {
            if (!issue.IsSuppressionInfo)
            {
                Diagnostic diag = new Diagnostic()
                {
                    Code = $"{ConfigHelpers.Section}: {issue.Rule.Id}",
                    Severity = DiagnosticSeverity.Error,
                    Message = $"{issue.Rule.Description ?? string.Empty}",
                    // DevSkim/Application Inspector line numbers are one-indexed, but column numbers are zero-indexed
                    // To get the diagnostic to appear on the correct line, we must subtract 1 from the line number
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
        foreach (CodeFixMapping codeFixMapping in codeFixes.ToArray())
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
            TextDocumentContentChangeEvent? content = request.ContentChanges.FirstOrDefault();
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
            TextDocumentItem content = request.TextDocument;
            return await GenerateDiagnosticsForTextDocument(content.Text, content.Version, request.TextDocument.Uri);
        }
        return Unit.Value;        
    }
    
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidCloseTextDocumentParams");
        if (StaticScannerSettings.RemoveFindingsOnClose)
        {
            _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
            {
                Diagnostics = new Container<Diagnostic>(),
                Uri = request.TextDocument.Uri,
                Version = null
            });
        }
        return Unit.Task;
    }
    
    public override async Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidSaveTextDocumentParams");
        if (StaticScannerSettings.ScanOnSave)
        {
            if (request.Text is null)
            {
                _logger.LogDebug("\tNo content found");
                return Unit.Value;
            }
            return await GenerateDiagnosticsForTextDocument(request.Text, null, request.TextDocument.Uri);
        }
        return Unit.Value;
    }
    
    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentSyncRegistrationOptions() {
        DocumentSelector = _documentSelector,
        Change = Change,
        Save = new SaveOptions() { IncludeText = true }
    };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        // TODO: This should return the correct language based on the uri
        if (StaticScannerSettings.RuleProcessorOptions.Languages.FromFileNameOut(uri.GetFileSystemPath(), out LanguageInfo Info))
        {
            return new TextDocumentAttributes(uri, Info.Name);
        }
        return new TextDocumentAttributes(uri, "unknown");
    }
}