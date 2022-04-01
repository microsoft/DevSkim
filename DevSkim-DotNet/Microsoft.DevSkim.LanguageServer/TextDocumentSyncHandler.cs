using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace DevSkim.LanguageServer;

class TextDocumentSyncHandler : ITextDocumentSyncHandler
{
    private readonly DocumentSelector _devSkimSupportedActionDocumentSelector = DocumentSelector.ForLanguage(new[] {"csharp"});
    
    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
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