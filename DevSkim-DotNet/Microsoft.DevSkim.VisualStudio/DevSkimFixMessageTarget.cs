namespace Microsot.DevSkim.LanguageClient
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using Microsoft.DevSkim.VisualStudio;
    using Newtonsoft.Json.Linq;
    using StreamJsonRpc;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
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
            //Clean out issues related to previous version of the file
            //_ = Task.Run(() =>
            //{
            //    var toRemoveKeys = StaticData.FileToCodeFixMap[mapping.fileName].Keys.Where(fileVersion => fileVersion < mapping.version);
            //    foreach (var key in toRemoveKeys)
            //    {
            //        StaticData.FileToCodeFixMap[mapping.fileName].TryRemove(key, out _);
            //    }
            //});
            
            return Task.CompletedTask;
        }
    }
}