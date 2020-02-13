// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// Factory for the <see cref="ITagger{T}"/>. There will be one instance of this class/VS session.
    /// 
    /// It is also the <see cref="ITableDataSource"/> that reports security errors in comments.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    internal sealed class DevSkimProvider : IViewTaggerProvider, ITableDataSource
    {
        internal readonly ITableManager ErrorTableManager;
        internal readonly ITextDocumentFactoryService TextDocumentFactoryService;
        internal readonly IClassifierAggregatorService ClassifierAggregatorService;

        const string _skimCheckerDataSource = "DevSkim";

        private readonly List<SinkManager> _managers = new List<SinkManager>();      // Also used for locks
        private readonly List<SkimChecker> _skimCheckers = new List<SkimChecker>();

        [ImportingConstructor]
        internal DevSkimProvider([Import]ITableManagerProvider provider, [Import] ITextDocumentFactoryService textDocumentFactoryService, [Import] IClassifierAggregatorService classifierAggregatorService)
        {
            this.ErrorTableManager = provider.GetTableManager(StandardTables.ErrorsTable);
            this.TextDocumentFactoryService = textDocumentFactoryService;

            this.ClassifierAggregatorService = classifierAggregatorService;

            this.ErrorTableManager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander, 
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName, StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column);
        }

        /// <summary>
        /// Create a tagger that does security checking on the view/buffer combination.
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            ITagger<T> tagger = null;

            // Only attempt to security check on the view's edit buffer (and multiple views could have that buffer open simultaneously so
            // only create one instance of the security checker.
            if ((buffer == textView.TextBuffer) && (typeof(T) == typeof(IErrorTag)))
            {
                var skimChecker = buffer.Properties.GetOrCreateSingletonProperty(typeof(SkimChecker), () => new SkimChecker(this, textView, buffer));

                // This is a thin wrapper around the SkimChecker that can be disposed of without shutting down the SkimChecker
                // (unless it was the last tagger on the skim checker).
                tagger = new DevSkimTagger(skimChecker) as ITagger<T>;
            }

            return tagger;
        }


        #region ITableDataSource members
        public string DisplayName
        {
            get
            {
                // This string should, in general, be localized since it is what would be displayed in any UI that lets the end user pick
                // which ITableDataSources should be subscribed to by an instance of the table control. It really isn't needed for the error
                // list however because it autosubscribes to all the ITableDataSources.
                return "DevSkim";
            }
        }

        public string Identifier
        {
            get
            {
                return _skimCheckerDataSource;
            }
        }

        public string SourceTypeIdentifier
        {
            get
            {
                return StandardTableDataSources.ErrorTableDataSource;
            }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            // This method is called to each consumer interested in errors. In general, there will be only a single consumer (the error list tool window)
            // but it is always possible for 3rd parties to write code that will want to subscribe.
            return new SinkManager(this, sink);
        }
        #endregion

        public void AddSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Add(manager);

                // Add the pre-existing skim checkers to the manager.
                foreach (var skimChecker in _skimCheckers)
                {
                    manager.AddSkimChecker(skimChecker);
                }
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Remove(manager);
            }
        }

        public void AddSkimChecker(SkimChecker skimChecker)
        {
            // This call will always happen on the UI thread (it is a side-effect of adding or removing the 1st/last tagger).
            lock (_managers)
            {
                _skimCheckers.Add(skimChecker);

                // Tell the preexisting managers about the new skim checker
                foreach (var manager in _managers)
                {
                    manager.AddSkimChecker(skimChecker);
                }
            }
        }

        public void RemoveSkimChecker(SkimChecker skimChecker)
        {
            // This call will always happen on the UI thread (it is a side-effect of adding or removing the 1st/last tagger).
            lock (_managers)
            {
                _skimCheckers.Remove(skimChecker);

                foreach (var manager in _managers)
                {
                    manager.RemoveSkimChecker(skimChecker);
                }
            }
        }

        public void UpdateAllSinks(SkimChecker skimChecker)
        {
            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.UpdateSink(skimChecker);
                }
            }
        }
    }
}
