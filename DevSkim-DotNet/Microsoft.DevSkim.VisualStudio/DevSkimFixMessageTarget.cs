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
        // TODO: This probably should be a circular queue of some kind to allow expired ones to eventually fall out
        private ConcurrentDictionary<string, ConcurrentDictionary<int?, HashSet<CodeFixMapping>>> _mappings = new ConcurrentDictionary<string, ConcurrentDictionary<int?, HashSet<CodeFixMapping>>>();
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
                () => { new ConcurrentDictionary<int?, HashSet<CodeFixMapping>>(new Dictionary<int?, HashSet<CodeFixMapping>>() { { mapping.version, new HashSet<CodeFixMapping>() { mapping } } }); }, 
                // Update
                (key, oldValue) =>
                {
                    oldValue.AddOrUpdate(mapping.version, 
                        // Add
                        () => { new HashSet<CodeFixMapping>() { mapping }; },
                        // Update
                        (versionKey, oldSet) => { oldSet.Add(mapping); return oldSet; });
                    return oldValue;
                });
            return Task.CompletedTask;
        }

        public ICollection<CodeFixMapping> Mappings { get { return _mappings; } }
    }
}