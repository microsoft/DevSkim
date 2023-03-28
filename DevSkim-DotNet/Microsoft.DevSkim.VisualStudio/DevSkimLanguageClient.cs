using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System.ComponentModel.Composition;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.DevSkim.LanguageProtoInterop;

namespace Microsot.DevSkim.LanguageClient
{
    // TODO: Test if code type also covers things like .json
    [ContentType("code")]
    [Export(typeof(ILanguageClient))]
    public class DevSkimLanguageClient : ILanguageClient, ILanguageClientCustomMessage2
    {
        [ImportingConstructor]
        public DevSkimLanguageClient()
        {
            _middleLayer = new DevSkimMiddleLayer();   
        }

        internal JsonRpc Rpc
        {
            get;
            set;
        }

        public object MiddleLayer = _middleLayer;
        private static object _middleLayer;

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public string Name => "DevSkim Language Extension";

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public bool ShowNotificationOnInitializeFailed => true;

        object ILanguageClientCustomMessage2.MiddleLayer => _middleLayer;

        public object CustomMessageTarget => _middleLayer;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Server", @"Microsoft.DevSkim.LanguageServer.exe");
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = info;

            if (process.Start())
            {
                return new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            }

            return null;
        }

        public async Task OnLoadedAsync()
        {
            if (StartAsync != null)
            {
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
            }
        }

        public async Task StopServerAsync()
        {
            if (StopAsync != null)
            {
                await StopAsync.InvokeAsync(this, EventArgs.Empty);
            }
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            this.Rpc = rpc;

            return Task.CompletedTask;
        }

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            string message = "DevSkim Language Client failed to activate.";
            string exception = initializationState.InitializationException?.ToString() ?? string.Empty;
            message = $"{message}\n {exception}";

            var failureContext = new InitializationFailureContext()
            {
                FailureMessage = message,
            };

            return Task.FromResult(failureContext);
        }

        internal class DevSkimMiddleLayer : ILanguageClientMiddleLayer
        {
            public bool CanHandle(string methodName)
            {
                var isOurs = methodName == DevSkimMessages.CodeFixMapping;
                return isOurs;
            }

            public Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
            {
                throw new NotImplementedException();
            }

            public async Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest)
            {
                var result = await sendRequest(methodParam);
                return result;
            }
        }
    }
}
