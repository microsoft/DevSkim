// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.DevSkim.VSExtension
{
    public class DevSkimErrorsSnapshot : WpfTableEntriesSnapshotBase
    {
        private readonly string _filePath;
        private readonly int _versionNumber;
        private readonly string _project;

        // We're not using an immutable list here but we cannot modify the list in any way once we've published the snapshot.
        public readonly List<DevSkimError> Errors = new List<DevSkimError>();

        public DevSkimErrorsSnapshot NextSnapshot;

        internal DevSkimErrorsSnapshot(string filePath, int versionNumber)
        {
            _filePath = filePath;
            _versionNumber = versionNumber;

            _project = VSPackage.GetProjectName(_filePath);
            _project = (_project != null) ? _project : string.Empty;            
        }

        public override int Count
        {
            get
            {
                return this.Errors.Count;
            }
        }

        public override int VersionNumber
        {
            get
            {
                return _versionNumber;
            }
        }

        public override int IndexOf(int currentIndex, ITableEntriesSnapshot newerSnapshot)
        {
            // This and TranslateTo() are used to map errors from one snapshot to a different one (that way the error list can do things like maintain the selection on an error
            // even when the snapshot containing the error is replaced by a new one).
            //
            // You only need to implement Identity() or TranslateTo() and, of the two, TranslateTo() is more efficient for the error list to use.

            // Map currentIndex to the corresponding index in newerSnapshot (and keep doing it until either
            // we run out of snapshots, we reach newerSnapshot, or the index can no longer be mapped forward).
            var currentSnapshot = this;            
            do
            {
                Debug.Assert(currentIndex >= 0);
                Debug.Assert(currentIndex < currentSnapshot.Count);

                currentIndex = currentSnapshot.Errors[currentIndex].NextIndex;

                currentSnapshot = currentSnapshot.NextSnapshot;
            }
            while ((currentSnapshot != null) && (currentSnapshot != newerSnapshot) && (currentIndex >= 0));

            return currentIndex;
        }

        public override bool TryGetValue(int index, string columnName, out object content)
        {
            string productName = "DevSkim";

            if ((index >= 0) && (index < this.Errors.Count))
            {
                if (columnName == StandardTableKeyNames.ProjectName)
                {
                    content = _project;
                    return true;
                }
                else if (columnName == StandardTableKeyNames.DocumentName)
                {
                    // We return the full file path here. The UI handles displaying only the Path.GetFileName().
                    content = _filePath;
                    return true;
                }                
                else if (columnName == StandardTableKeyNames.ErrorCategory)
                {
                    content = "Security";
                    return true;
                }
                else if (columnName == StandardTableKeyNames.ErrorSource)
                {
                    content = productName;
                    return true;
                }
                else if (columnName == StandardTableKeyNames.Line)
                {
                    // Line and column numbers are 0-based (the UI that displays the line/column number will add one to the value returned here).
                    content = this.Errors[index].Span.Start.GetContainingLine().LineNumber;

                    return true;
                }
                else if (columnName == StandardTableKeyNames.Column)
                {
                    var position = this.Errors[index].Span.Start;
                    var line = position.GetContainingLine();
                    content = position.Position - line.Start.Position;

                    return true;
                }
                else if (columnName == StandardTableKeyNames.Text)
                {
                    content = string.Format(CultureInfo.InvariantCulture, "{0}: [{1}] {2}", productName, this.Errors[index].Rule.Severity, this.Errors[index].Rule.Name);

                    return true;
                }
                else if (columnName == StandardTableKeyNames.ErrorSeverity)
                {
                    if (!this.Errors[index].Actionable)
                    {
                        content = __VSERRORCATEGORY.EC_MESSAGE;
                    }
                    else
                    {
                        switch (this.Errors[index].Rule.Severity)
                        {
                            case Severity.Critical:
                            case Severity.Important:
                            case Severity.Moderate:
                                content = __VSERRORCATEGORY.EC_ERROR;
                                break;                            
                            case Severity.BestPractice:                            
                                content = __VSERRORCATEGORY.EC_WARNING;
                                break;
                            default:
                                content = __VSERRORCATEGORY.EC_MESSAGE;
                                break;
                        }
                    }

                    return true;
                }
                else if (columnName == StandardTableKeyNames.ErrorSource)
                {
                    content = ErrorSource.Other;

                    return true;
                }
                else if (columnName == StandardTableKeyNames.BuildTool)
                {
                    content = productName;

                    return true;
                }
                else if (columnName == StandardTableKeyNames.ErrorCode)
                {                    
                    content = this.Errors[index].Rule.Id;

                    return true;
                }
                else if ((columnName == StandardTableKeyNames.ErrorCodeToolTip) || (columnName == StandardTableKeyNames.HelpLink))
                {
                    content = this.Errors[index].Rule.RuleInfo;

                    return true;
                }

                // We should also be providing values for StandardTableKeyNames.Project & StandardTableKeyNames.ProjectName but that is
                // beyond the scope of this sample.
            }
             
            content = null;
            return false;
        }

        public override bool CanCreateDetailsContent(int index)
        {
            return true;
        }

        public override bool TryCreateDetailsStringContent(int index, out string content)
        {
            content = this.Errors[index].Rule.Description;
            return (content != null);
        }
    }
}
