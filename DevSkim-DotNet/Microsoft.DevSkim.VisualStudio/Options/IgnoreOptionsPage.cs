namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Shell;
    using System.Collections.Generic;
    using System.ComponentModel;

    public class IgnoreOptionsPage : DialogPage
    {


        [Category("Ignore Options")]
        [DisplayName("Ignore Files")]
        [Description("Regular expressions to exclude files and folders from analysis.")]
        public List<string> IgnoreFiles
        {
            get; set;
        } = new List<string>();

        [Category("Ignore Options")]
        [DisplayName("Ignore Rules List")]
        [Description("DevSkim Rule IDs to ignore.")]
        public List<string> IgnoreRulesList
        {
            get; set;
        } = new List<string>
        {
            "\\.(exe|dll|so|dylib|bin|so\\..*)$",
            "\\.(png|jpg|jpeg|gif|psd|ico|mp3|mpeg|bmp)$",
            "\\.(zip|tar|gz|rar|jar|gz|7z|bz|bz2|gzip|cab|war|xz|nupkg|gem|egg)$",
            "\\.(sqlite3|db)$",
            "(^|/)(out|bin)/",
            "(^|/)(tests?|unittests?|__tests__|__mocks__)/",
            "(^|/)(\\.git|git)/",
            "\\.(git|git[^./])$",
            "-lock\\.[^/]|\\.lock$",
            "(^|/)(\\.vscode|\\.cache|logs)/",
            "(^|/)(nuget|node_modules)/",
            "\\.(log|sarif|test)$",
            "\\.(py[cod])$",
            "(^|/)__pycache__/"
        };

        [Category("Ignore Options")]
        [DisplayName("Ignore Default Rules")]
        [Description("Disable all default DevSkim rules.")]
        public bool IgnoreDefaultRules
        {
            get; set;
        } = false;
    }
}
