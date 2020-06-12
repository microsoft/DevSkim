// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.DevSkim.VSExtension
{
    internal class DevSkimErrorsFactory : TableEntriesSnapshotFactoryBase
    {
        public DevSkimErrorsFactory(SkimChecker skimChecker, DevSkimErrorsSnapshot securityErrors)
        {
            _skimChecker = skimChecker;

            this.CurrentSnapshot = securityErrors;
        }

        public DevSkimErrorsSnapshot CurrentSnapshot { get; private set; }

        public override int CurrentVersionNumber
        {
            get
            {
                return this.CurrentSnapshot.VersionNumber;
            }
        }

        public override void Dispose()
        {
        }

        public override ITableEntriesSnapshot GetCurrentSnapshot()
        {
            return this.CurrentSnapshot;
        }

        public override ITableEntriesSnapshot GetSnapshot(int versionNumber)
        {
            // In theory the snapshot could change in the middle of the return statement so snap the snapshot
            // just to be safe.
            var snapshot = this.CurrentSnapshot;
            return (versionNumber == snapshot.VersionNumber) ? snapshot : null;
        }

        internal void UpdateErrors(DevSkimErrorsSnapshot securityErrors)
        {
            this.CurrentSnapshot.NextSnapshot = securityErrors;
            this.CurrentSnapshot = securityErrors;
        }

        private readonly SkimChecker _skimChecker;
    }
}