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

        /// <summary>
        /// Remove all Code fixes for the specified filename that are not of the specified version
        /// </summary>
        /// <param name="token">JToken representation of <see cref="MappingsVersion"/></param>
        /// <returns></returns>
        [JsonRpcMethod(DevSkimMessages.FileVersion)]
        public async Task RemoveOldMappingsByVersionAsync(JToken token)
        {
            await Task.Run(() =>
            {
                MappingsVersion version = token.ToObject<MappingsVersion>();
                if (version is { })
                {
                    if (StaticData.FileToCodeFixMap.ContainsKey(version.fileName))
                    {
                        foreach (var key in StaticData.FileToCodeFixMap[version.fileName].Keys)
                        {
                            if (key != version.version)
                            {
                                StaticData.FileToCodeFixMap[version.fileName].TryRemove(key, out _);
                            }
                        }
                    }
                }
            });
        }


        /// <summary>
        /// Update the client cache of available fixes for published diagnostics
        /// </summary>
        /// <param name="jToken">JToken representation of <see cref="CodeFixMapping"/></param>
        /// <returns></returns>
        [JsonRpcMethod(DevSkimMessages.CodeFixMapping)]
        public async Task CodeFixMappingEventAsync(JToken jToken)
        {
            await Task.Run(() =>
            {
                CodeFixMapping mapping = jToken.ToObject<CodeFixMapping>();
                if (mapping is { })
                {
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
                }
            });
        }
    }
}