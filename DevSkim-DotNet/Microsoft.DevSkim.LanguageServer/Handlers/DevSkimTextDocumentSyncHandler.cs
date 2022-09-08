// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Microsoft.DevSkim.LanguageServer.Handlers
{
    public class DevSkimTextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        private readonly ILanguageServerFacade _facade;

        public DevSkimTextDocumentSyncHandler(ILanguageServerFacade facade)
        {
            _facade = facade;
        }

        public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken token)
        {
            // we have full sync enabled, so apparently first change is the whole document
            var contents = request.ContentChanges.First().Text;

            var documentUri = request.TextDocument.Uri;

            // DevSkim

            // Publish Diagnostics
            var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();

            diagnostics.Add(new Diagnostic()
            {
                Code = "ErrorCode_001",
                Severity = DiagnosticSeverity.Error,
                Message = "Something bad happened",
                Range = new Range(0, 0, 0, 0),
                Source = "XXX",
                Tags = new Container<DiagnosticTag>(new DiagnosticTag[] { DiagnosticTag.Unnecessary })
            });

            _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams() 
            {
                Diagnostics = new Container<Diagnostic>(diagnostics.ToArray()),
                Uri = request.TextDocument.Uri,
                Version = request.TextDocument.Version
            });

            return Unit.Task;
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            // Need to return correct language name
            return new TextDocumentAttributes(uri, "csharp");
        }

        public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentUri = request.TextDocument.Uri;

            // DevSkim if scan on open is enabled
            return Unit.Task;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentUri = request.TextDocument.Uri;

            // Remove findings from closed document?

            return Unit.Task;
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            Change = TextDocumentSyncKind.Full,
            DocumentSelector = DocumentSelector.ForLanguage(new []{"csharp"})
        };
    }
}
