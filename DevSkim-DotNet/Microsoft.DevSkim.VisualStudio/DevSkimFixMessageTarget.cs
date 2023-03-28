namespace Microsot.DevSkim.LanguageClient
{
    using Microsoft.DevSkim.LanguageProtoInterop;
    using Microsoft.VisualStudio.Telemetry;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using StreamJsonRpc;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class DevSkimFixMessageTarget
    {
        private HashSet<CodeFixMapping> _mappings = new HashSet<CodeFixMapping>();
        public DevSkimFixMessageTarget()
        {
        }

        [JsonRpcMethod(DevSkimMessages.CodeFixMapping)]
        public Task HandleTelemetryEventAsync(JToken jToken)
        {
            var mapping = jToken.ToObject<CodeFixMapping>();
            _mappings.Add(mapping);
            return Task.CompletedTask;
        }

        public ICollection<CodeFixMapping> Mappings { get { return _mappings; } }
    }
}