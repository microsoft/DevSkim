using System.Collections.Immutable;
using DiscUtils.Streams;
using MediatR;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.DevSkim;
using Microsoft.Extensions.Logging.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace DevSkim.LanguageServer;

class TextDocumentSyncHandler : ITextDocumentSyncHandler
{
    private readonly DocumentSelector _devSkimSupportedActionDocumentSelector = DocumentSelector.ForLanguage(new[] {"csharp"});

    private readonly ILanguageServerFacade _facade;

    public TextDocumentSyncHandler(ILanguageServerFacade facade)
    {
        _facade = facade;
        DevSkimRuleSet devSkimRuleSet =  DevSkimRuleSet.GetDefaultRuleSet();
        Languages devSkimLanguages = DevSkimLanguages.LoadEmbedded();

        Severity severityFilter = Severity.Critical | Severity.Important | Severity.Moderate | Severity.ManualReview;

        Confidence confidenceFilter = Confidence.High | Confidence.Medium;

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

    private DevSkimRuleProcessor _processor;
    
    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var content = request.ContentChanges.First();
        if (content == null)
        {
            return Unit.Task;
        }
        var issues = _processor.Analyze(content.Text, request.TextDocument.Uri.Path).ToList();

        // Diagnostics are sent a document at a time, this example is for demonstration purposes only
        var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();

        foreach (var issue in issues)
        {
            diagnostics.Add(new Diagnostic()
            {
                Code = issue.Rule.Id,
                Severity = DiagnosticSeverity.Error,
                Message = issue.Rule.Description,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(issue.StartLocation.Line, issue.StartLocation.Column, issue.EndLocation.Line, issue.EndLocation.Column),
                Source = "DevSkim Language Server"
            });
        }

        _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams() 
        {
            Diagnostics = new Container<Diagnostic>(diagnostics.ToArray()),
            Uri = request.TextDocument.Uri,
            Version = request.TextDocument.Version
        });

        return Unit.Task;
    }

    TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentChangeRegistrationOptions()
        {
            DocumentSelector = _devSkimSupportedActionDocumentSelector,
            SyncKind = TextDocumentSyncKind.Full
        };
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentOpenRegistrationOptions()
        {
            DocumentSelector = _devSkimSupportedActionDocumentSelector
        };
    }

    public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentCloseRegistrationOptions();
    }

    
    public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSaveRegistrationOptions()
        {
            IncludeText = false
        };
    }

    public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        throw new NotImplementedException();
    }
}