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
            RunIds = new List<string>();
            FileLocations = new Dictionary<string, Dictionary<string, string>>();
        }

        public Results(Dictionary<string, string> ResultLocations, List<string> RunIds, Dictionary<string, Dictionary<string, string>> FileLocations)
        {
            this.ResultLocations = ResultLocations;
            this.RunIds = RunIds;
            this.FileLocations = FileLocations;
        }
        // Maps RunId to Location
        public Dictionary<string, string> ResultLocations { get; set; }
        public List<string> RunIds { get; set; }
        // Maps RunId to Dictionary of FileName to Location
        public Dictionary<string, Dictionary<string, string>> FileLocations { get; set; }
    }
}
