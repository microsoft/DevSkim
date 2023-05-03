﻿namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class GuidanceOptionsPage : DialogPage
    {
        [Category("Guidance Options")]
        [DisplayName("Guidance Base URL")]
        [Description("Each finding has a guidance file that describes the issue and solutions in more detail.  " +
            "By default, those files live on the DevSkim github repo however, with this setting, " +
            "organizations can clone and customize that repo, and specify their own base URL for the guidance.")]
        public string GuidanceBaseURL
        {
            get; set;
        } = "https://github.com/Microsoft/DevSkim/blob/main/guidance/";
    }
}
