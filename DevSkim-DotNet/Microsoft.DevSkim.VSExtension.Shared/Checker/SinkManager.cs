// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Shell.TableManager;
using System;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    ///     Every consumer of data from an <see cref="ITableDataSource"/> provides an <see
    ///     cref="ITableDataSink"/> to record the changes. We give the consumer an IDisposable (this object)
    ///     that they hang on to as long as they are interested in our data (and they Dispose() of it when
    ///     they are done).
    /// </summary>
    internal class SinkManager : IDisposable
    {
        public void Dispose()
        {
            // Called when the person who subscribed to the data source disposes of the cookie (== this
            // object) they were given.
            _securityErrorsProvider.RemoveSinkManager(this);
        }

        internal SinkManager(DevSkimProvider securityErrorsProvider, ITableDataSink sink)
        {
            _securityErrorsProvider = securityErrorsProvider;
            _sink = sink;

            securityErrorsProvider.AddSinkManager(this);
        }

        internal void AddSkimChecker(SkimChecker skimChecker)
        {
            _sink.AddFactory(skimChecker.Factory);
        }

        internal void RemoveSkimChecker(SkimChecker skimChecker)
        {
            _sink.RemoveFactory(skimChecker.Factory);
        }

        internal void UpdateSink(SkimChecker skimChecker)
        {
            _sink.FactorySnapshotChanged(skimChecker.Factory);
        }

        private readonly DevSkimProvider _securityErrorsProvider;
        private readonly ITableDataSink _sink;
    }
}