using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DevSkim.Blazor
{
    public class Results
    {
        // Maps RunId to Location
        public Dictionary<string, string> ResultLocations = new Dictionary<string, string>();
        public List<string> RunIds = new List<string>();
        // Maps RunId to Dictionary of FileName to Location
        public Dictionary<string, Dictionary<string, string>> FileLocations = new Dictionary<string, Dictionary<string, string>>();
    }
}
