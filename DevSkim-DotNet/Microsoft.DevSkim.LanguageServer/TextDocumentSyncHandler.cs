using System.Collections.Immutable;
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

        DevSkimRuleSet devSkimRuleSet =  DevSkimRuleSet.GetDefaultRuleSet();
        Languages devSkimLanguages = DevSkimLanguages.LoadEmbedded();
        Severity severityFilter = Severity.Critical | Severity.Important | Severity.Moderate | Severity.ManualReview;
        Confidence confidenceFilter = Confidence.High | Confidence.Medium;

        // To avoid enumerating rules and languages in release configurations
        #if DEBUG
            _logger.LogDebug("TextDocumentSyncHandler.cs: ctor");
            _logger.LogDebug("\tLoaded DevSkim configurations:");
            _logger.LogDebug($"\t\tRuleSet:");
            devSkimRuleSet.ToList().ForEach(x => _logger.LogDebug("\t\t\t" + x?.Id + " - " + x?.Name));
            _logger.LogDebug($"\t\tLanguages:");
            devSkimLanguages.GetNames().ToList().ForEach(x => _logger.LogDebug("\t\t\t" + x));
            _logger.LogDebug($"\t\tSeverityFilter: {severityFilter}");
            _logger.LogDebug($"\t\tConfidenceFilter: {confidenceFilter}");
        #endif

        // Initialize the processor
        var devSkimRuleProcessorOptions = new DevSkimRuleProcessorOptions()
        {
            Languages = devSkimLanguages,
            AllowAllTagsInBuildFiles = true,
            LoggerFactory = NullLoggerFactory.Instance,
            Parallel = true,
            SeverityFilter = severityFilter,
            ConfidenceFilter = confidenceFilter,
        };

        _processor = new DevSkimRuleProcessor(devSkimRuleSet, devSkimRuleProcessorOptions);
        _processor.EnableSuppressions = true;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;
    
    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidChangeTextDocumentParams");

        var content = request.ContentChanges.First();
        if (content == null)
        {
            _logger.LogDebug("\tNo content found");
            return Unit.Task;
        }

        // Diagnostics are sent a document at a time
        _logger.LogDebug($"\tProcessing document: {request.TextDocument.Uri.Path}");
        _logger.LogDebug($"\twith content:\n{content.Text}");
        var issues = _processor.Analyze(content.Text, request.TextDocument.Uri.Path).ToList();
        var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();

        _logger.LogDebug($"\tAdding {issues.Count} issues to diagnostics");
        foreach (var issue in issues)
        {
            diagnostics.Add(new Diagnostic()
            {
                Code = issue.Rule.Id,
                Severity = DiagnosticSeverity.Error,
                Message = issue.Rule.Description,
                Range = new Range(issue.StartLocation.Line-1, issue.StartLocation.Column, issue.EndLocation.Line-1, issue.EndLocation.Column),
                Source = "DevSkim Language Server"
            });
        }

        _logger.LogDebug("\tPublishing diagnostics...");
        _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams() 
        {
            Diagnostics = new Container<Diagnostic>(diagnostics.ToArray()),
            Uri = request.TextDocument.Uri,
            Version = request.TextDocument.Version
        });

        return Unit.Task;
    }

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidOpenTextDocumentParams");
        await Task.Yield();
        await _configuration.GetScopedConfiguration(request.TextDocument.Uri, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
    
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidCloseTextDocumentParams");
        if (_configuration.TryGetScopedConfiguration(request.TextDocument.Uri, out var disposable))
        {
            disposable.Dispose();
        }

        return Unit.Task;
    }
    
    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("TextDocumentSyncHandler.cs: DidSaveTextDocumentParams");
        return Unit.Task;
    }
    
    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new TextDocumentSyncRegistrationOptions() {
        DocumentSelector = _documentSelector,
        Change = Change,
        Save = new SaveOptions() { IncludeText = false }
    };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "csharp");
    }
}