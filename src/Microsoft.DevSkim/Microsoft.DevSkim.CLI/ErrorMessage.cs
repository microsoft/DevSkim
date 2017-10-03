using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DevSkim.CLI
{
    class ErrorMessage
    {
        public string File { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public string RuleID { get; set; }
        public bool Warning { get; set; }
    }
}
