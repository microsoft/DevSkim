// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using MediatR;
using Microsoft.DevSkim;
using Microsoft.DevSkim.LanguageProtoInterop;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DevSkim.LanguageServer
{
    /// <summary>
    /// Handles textDocument/codeAction requests from the LSP client.
    /// Provides quick fixes for DevSkim security findings.
    /// </summary>
    internal class CodeActionHandler : ICodeActionHandler
    {
        private readonly ILogger<CodeActionHandler> _logger;
        
        // Store code fixes keyed by document URI and diagnostic key
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<CodeFixMapping>>> _codeFixCache = new();
        
        // Store line lengths per document so we can compute exact end-of-line positions for suppressions
        private static readonly ConcurrentDictionary<string, int[]> _lineLengthCache = new();

        public CodeActionHandler(ILogger<CodeActionHandler> logger)
        {
            _logger = logger;
        }

        public CodeActionRegistrationOptions GetRegistrationOptions(CodeActionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new CodeActionRegistrationOptions
            {
                DocumentSelector = TextDocumentSelector.ForPattern("**/*"),
                CodeActionKinds = new Container<CodeActionKind>(CodeActionKind.QuickFix),
                ResolveProvider = false
            };
        }

        public Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
        {
            var result = new List<CommandOrCodeAction>();
            var documentUri = request.TextDocument.Uri.ToString();

            _logger.LogDebug($"CodeActionHandler: Processing request for {documentUri}");
            _logger.LogDebug($"CodeActionHandler: {request.Context.Diagnostics.Count()} diagnostics in context");

            foreach (var diagnostic in request.Context.Diagnostics)
            {
                // Only process DevSkim diagnostics
                if (diagnostic.Source != "DevSkim Language Server")
                {
                    continue;
                }

                var diagnosticKey = CreateDiagnosticKey(documentUri, diagnostic);

                if (_codeFixCache.TryGetValue(documentUri, out var documentFixes) &&
                    documentFixes.TryGetValue(diagnosticKey, out var fixes))
                {
                    _logger.LogDebug($"CodeActionHandler: Found {fixes.Count} fixes");
                    foreach (var fix in fixes)
                    {
                        var codeAction = new CodeAction
                        {
                            Title = fix.friendlyString,
                            Kind = CodeActionKind.QuickFix,
                            Diagnostics = new Container<Diagnostic>(diagnostic),
                            Edit = CreateWorkspaceEdit(request.TextDocument.Uri, diagnostic, fix)
                        };
                        result.Add(codeAction);
                    }
                }
            }

            _logger.LogDebug($"CodeActionHandler: Returning {result.Count} code actions");
            return Task.FromResult<CommandOrCodeActionContainer?>(new CommandOrCodeActionContainer(result));
        }

        private static WorkspaceEdit CreateWorkspaceEdit(DocumentUri uri, Diagnostic diagnostic, CodeFixMapping fix)
        {
            TextEdit textEdit;
            if (fix.isSuppression)
            {
                // For suppressions, insert at the actual end of the line
                int line = diagnostic.Range.End.Line;
                int lineLength = GetLineLength(uri, line);
                textEdit = new TextEdit
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(line, lineLength, line, lineLength),
                    NewText = fix.replacement
                };
            }
            else
            {
                textEdit = new TextEdit
                {
                    Range = diagnostic.Range,
                    NewText = fix.replacement
                };
            }

            return new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    [uri] = new[] { textEdit }
                }
            };
        }

        /// <summary>
        /// Store line lengths for a document, computed from the document text we already have during scanning.
        /// </summary>
        public static void SetLineLengths(DocumentUri uri, string text)
        {
            var lines = text.Split('\n');
            var lengths = new int[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                lengths[i] = lines[i].TrimEnd('\r').Length;
            }
            _lineLengthCache[uri.ToString()] = lengths;
        }

        private static int GetLineLength(DocumentUri uri, int line)
        {
            if (_lineLengthCache.TryGetValue(uri.ToString(), out var lengths) && line < lengths.Length)
            {
                return lengths[line];
            }
            return 0;
        }

        /// <summary>
        /// Register code fixes for a diagnostic. Called by TextDocumentSyncHandler when processing documents.
        /// </summary>
        public static void RegisterCodeFix(DocumentUri uri, Diagnostic diagnostic, CodeFixMapping fix)
        {
            var documentUri = uri.ToString();
            var diagnosticKey = CreateDiagnosticKey(documentUri, diagnostic);

            var documentFixes = _codeFixCache.GetOrAdd(documentUri, _ => new ConcurrentDictionary<string, List<CodeFixMapping>>());
            var fixes = documentFixes.GetOrAdd(diagnosticKey, _ => new List<CodeFixMapping>());
            
            lock (fixes)
            {
                fixes.Add(fix);
            }
        }

        /// <summary>
        /// Clear all code fixes for a document. Called when document is closed or rescanned.
        /// </summary>
        public static void ClearCodeFixes(DocumentUri uri)
        {
            _codeFixCache.TryRemove(uri.ToString(), out _);
            _lineLengthCache.TryRemove(uri.ToString(), out _);
        }

        private static string CreateDiagnosticKey(string documentUri, Diagnostic diagnostic)
        {
            return $"{documentUri}: {diagnostic.Message}, {diagnostic.Code}, {diagnostic.Range.Start.Line}, {diagnostic.Range.Start.Character}, {diagnostic.Range.End.Line}, {diagnostic.Range.End.Character}";
        }
    }
}
