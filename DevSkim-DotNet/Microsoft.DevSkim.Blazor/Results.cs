using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DevSkim.Blazor
{
    public class Results
    {
        public Results() {
            ResultLocations = new Dictionary<string, string>();
            FileLocations = new Dictionary<string, Dictionary<string, string>>();
            RunIdMap = new Dictionary<string, string>();
        }

        public Results(Dictionary<string, string> ResultLocations, Dictionary<string, Dictionary<string, string>> FileLocations, Dictionary<string, string> RunIdMap)
        {
            this.ResultLocations = ResultLocations;
            this.FileLocations = FileLocations;
            this.RunIdMap = RunIdMap;
        }
        // Maps RunId to Location
        public Dictionary<string, string> ResultLocations { get; set; }
        // Maps RunId to Dictionary of FileName to Location
        public Dictionary<string, Dictionary<string, string>> FileLocations { get; set; }
        public Dictionary<string, string> RunIdMap { get; set; }
    }
}
