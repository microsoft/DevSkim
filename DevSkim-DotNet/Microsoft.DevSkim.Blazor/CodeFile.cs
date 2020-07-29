using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DevSkim.Blazor
{
    public class CodeFile
    {
        public CodeFile(string Content, string FileName, string RunId)
        {
            this.Content = Content;
            this.FileName = FileName;
            this.RunId = RunId;
        }
        public string Content { get; set; }
        public string FileName { get; set; }
        public string RunId { get; set; }
    }
}
