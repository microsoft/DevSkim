namespace Microsot.DevSkim.LanguageClient
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using Microsoft.DevSkim.VisualStudio;
    using Newtonsoft.Json.Linq;
    using StreamJsonRpc;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class DevSkimFixMessageTarget
    {
        public DevSkimFixMessageTarget()
        {
        }

        // Mark the method with the JSONRPC call this will parse
        [JsonRpcMethod(DevSkimMessages.CodeFixMapping)]
        public Task CodeFixMappingEventAsync(JToken jToken)
        {
            CodeFixMapping mapping = jToken.ToObject<CodeFixMapping>();
            StaticData.FileToCodeFixMap.AddOrUpdate(mapping.fileName, 
                // Add New Nested Dictionary
                (Uri _) => new ConcurrentDictionary<int, HashSet<CodeFixMapping>>(new Dictionary<int, HashSet<CodeFixMapping>>() { { mapping.version ?? -1, new HashSet<CodeFixMapping>() { mapping } } }),
                // Update Nested Dictionary
                (key, oldValue) =>
                {
                    oldValue.AddOrUpdate(mapping.version ?? -1, 
                        // Add new HashSet
                        (int _) => new HashSet<CodeFixMapping>() { mapping },
                        // Update HashSet of CodeFixMappings
                        (versionKey, oldSet) => { oldSet.Add(mapping); return oldSet; });
                    return oldValue;
                });
            return Task.CompletedTask;
        }
    }
}