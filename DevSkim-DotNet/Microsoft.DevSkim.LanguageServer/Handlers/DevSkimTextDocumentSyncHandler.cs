// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.DevSkim.LanguageServer.Handlers
{
    public class DevSkimTextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        private readonly ILanguageServerFacade _facade;
        private readonly DevSkimRuleProcessor _processor;
        private readonly Languages _languages;

        public DevSkimTextDocumentSyncHandler(ILanguageServerFacade facade)
        {
            _facade = facade;
            // Initialize the processor
            // _languages = DevSkimLanguages.LoadEmbedded();
            // var devSkimRuleProcessorOptions = new DevSkimRuleProcessorOptions()
            // {
            //     Languages = _languages,
            //     AllowAllTagsInBuildFiles = true,
            //     LoggerFactory = NullLoggerFactory.Instance,
            //     Parallel = false,
            //     // SeverityFilter = severityFilter,
            //     // ConfidenceFilter = confidenceFilter
            // };
            // var devSkimRuleSet = DevSkimRuleSet.GetDefaultRuleSet();

            // _processor = new DevSkimRuleProcessor(devSkimRuleSet, devSkimRuleProcessorOptions);
            
        }

        public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken token)
        {
            // we have full sync enabled, so apparently first change is the whole document
            var contents = request.ContentChanges.First().Text;

            var documentUri = request.TextDocument.Uri;
            // Publish Diagnostics
            var diagnostics = ImmutableArray<Diagnostic>.Empty.ToBuilder();
            diagnostics.Add(new Diagnostic()
            {
                Code = "ErrorCode_001",
                Severity = DiagnosticSeverity.Error,
                Message = "Something bad happened",
                Range = new Range(1, 1, 1, 1),
                Source = "XXX",
                Tags = new Container<DiagnosticTag>(new DiagnosticTag[] { DiagnosticTag.Unnecessary })
            });
            // DevSkim
            // var results = _processor.Analyze(contents, documentUri.Path);
            //
            // foreach (var result in results)
            // {
            //     diagnostics.Add(new Diagnostic()
            //     {
            //         Code = result.Rule.Id,
            //         Severity = SeverityToDiagnosticSeverity(result.Rule.Severity),
            //         Message = result.Rule.Description ?? string.Empty,
            //         Range = new Range(result.StartLocation.Line, result.StartLocation.Column, result.EndLocation.Line, result.EndLocation.Column),
            //         Source = result.Rule.Source,
            //     });
            // }
            
            _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams() 
            {
                Diagnostics = new Container<Diagnostic>(diagnostics.ToArray()),
                Uri = request.TextDocument.Uri,
                Version = request.TextDocument.Version
            });

            return Unit.Task;
        }

        private DiagnosticSeverity? SeverityToDiagnosticSeverity(Severity ruleSeverity)
        {
            return ruleSeverity switch
            {
                Severity.Unspecified => DiagnosticSeverity.Hint,
                Severity.Critical => DiagnosticSeverity.Error,
                Severity.Important => DiagnosticSeverity.Warning,
                Severity.Moderate => DiagnosticSeverity.Information,
                Severity.BestPractice => DiagnosticSeverity.Hint,
                Severity.ManualReview => DiagnosticSeverity.Information,
                _ => throw new ArgumentOutOfRangeException(nameof(ruleSeverity), ruleSeverity, null)
            };
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            // Need to return correct language name
            var languageInfo = _languages.FromFileNameOut(uri.Path, out LanguageInfo info);
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
            DocumentSelector = DocumentSelector.ForLanguage(new []{"c",
                "clojure",
                "coffeescript",
                "cpp",
                "csharp",
                "fsharp",
                "go",
                "groovy",
                "jade",
                "java",
                "javascript",
                "javascriptreact",
                "lua",
                "objective-c",
                "perl",
                "perl6",
                "php",
                "plaintext",
                "powershell",
                "python",
                "r",
                "ruby",
                "rust",
                "shellscript",
                "sql",
                "swift",
                "typescript",
                "typescriptreact",
                "vb",
                "xml",
                "yaml"})
        };
    }
}
