// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.DevSkim.VSExtension
{
    /// <summary>
    /// DevSkim specifig tag
    /// </summary>
    class DevSkimTag : IErrorTag
    {
        /// <summary>
        /// Tag
        /// </summary>
        /// <param name="rule">Rule associated with the tag</param>
        /// <param name="actionable">Is error actionable</param>
        public DevSkimTag(Rule rule, bool actionable)
        {
            Rule = rule;
            if (actionable)
                ErrorType = PredefinedErrorTypeNames.OtherError;
            else
                ErrorType = PredefinedErrorTypeNames.Warning;

            ToolTipContent = new DevSkimToolTip(Rule);
        }

        public string ErrorType { get; set; }

        public object ToolTipContent { get; set; }

        public Rule Rule { get; set; }
    }
}
