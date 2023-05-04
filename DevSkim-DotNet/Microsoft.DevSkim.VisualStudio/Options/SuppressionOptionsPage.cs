namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Windows.Media.Media3D;

    [Guid(PageGuidString)]
    public class SuppressionOptionsPage : DialogPage
    {
        public const string PageGuidString = "17fdabd9-f356-3ed3-96d6-433bcc49ec1a";

        [Category("Suppression Options")]
        [DisplayName("Suppression Duration In Days")]
        [Description("DevSkim allows for findings to be suppressed for a temporary period of time. " +
            "The default is 30 days. Set to 0 to disable temporary suppressions.")]
        public int SuppressionDurationInDays
        {
            get; set;
        } = 30;
        
        public enum CommentStylesEnum
        {
            Line,
            Block
        }
        [Category("Suppression Options")]
        [DisplayName("Suppression Comment Style")]
        [Description("When DevSkim inserts a suppression comment it defaults to using single line comments for " +
            "every language that has them.  Setting this to 'block' will instead use block comments for the languages " +
            "that support them.  Block comments are suggested if regularly adding explanations for why a finding " +
            "was suppressed")]
        public CommentStylesEnum SuppressionCommentStyle
        {
            get; set;
        } = CommentStylesEnum.Line;

        [Category("Suppression Options")]
        [DisplayName("Manual Reviewer Name")]
        [Description("If set, insert this name in inserted suppression comments.")]
        public string ManualReviewerName
        {
            get; set;
        } = "";
    }
}
