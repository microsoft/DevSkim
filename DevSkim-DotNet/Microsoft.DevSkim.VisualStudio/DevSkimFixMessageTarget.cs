namespace Microsot.DevSkim.LanguageClient
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using Newtonsoft.Json.Linq;
    using StreamJsonRpc;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class DevSkimFixMessageTarget
    {
        // Maps file name to a dictionary of file versions to a deduplicated set of CodeFixMappings
        public ConcurrentDictionary<string, ConcurrentDictionary<int?, HashSet<CodeFixMapping>>> _mappings = new ConcurrentDictionary<string, ConcurrentDictionary<int?, HashSet<CodeFixMapping>>>();
        public DevSkimFixMessageTarget()
        {
        }

        // Mark the method with the JSONRPC call this will parse
        [JsonRpcMethod(DevSkimMessages.CodeFixMapping)]
        public Task CodeFixMappingEventAsync(JToken jToken)
        {
            CodeFixMapping mapping = jToken.ToObject<CodeFixMapping>();
            _mappings.AddOrUpdate(mapping.fileName, 
                // Add
                (string _) => new ConcurrentDictionary<int?, HashSet<CodeFixMapping>>(new Dictionary<int?, HashSet<CodeFixMapping>>() { { mapping.version, new HashSet<CodeFixMapping>() { mapping } } }),
                // Update
                (key, oldValue) =>
                {
                    oldValue.AddOrUpdate(mapping.version, 
                        // Add
                        (int? _) => new HashSet<CodeFixMapping>() { mapping },
                        // Update
                        (versionKey, oldSet) => { oldSet.Add(mapping); return oldSet; });
                    return oldValue;
                });
            return Task.CompletedTask;
        }
    }
}