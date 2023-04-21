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
using Microsoft.DevSkim.VisualStudio.ProcessTracker;

namespace Microsot.DevSkim.LanguageClient
{
    // TODO: Test if code type also covers things like .json
    [ContentType("code")]
    [Export(typeof(ILanguageClient))]
    public class DevSkimLanguageClient : ILanguageClient, ILanguageClientCustomMessage2
    {
        [ImportingConstructor]
        public DevSkimLanguageClient(IProcessTracker processTracker)
        {
            _processTracker = processTracker;
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

        // This handles modifying outgoing messages to the language server
        object ILanguageClientCustomMessage2.MiddleLayer => null;

        private DevSkimFixMessageTarget DevSkimTarget = new DevSkimFixMessageTarget();

        // This handles incoming messages to the language client
        public object CustomMessageTarget => DevSkimTarget;
        private readonly IProcessTracker _processTracker;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Server", @"Microsoft.DevSkim.LanguageServer.exe");
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.Arguments = "--visual-studio";

            Process process = new Process();
            process.StartInfo = info;

            if (process.Start())
            {
                _processTracker.AddProcess(process);
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

            InitializationFailureContext failureContext = new InitializationFailureContext()
            {
                FailureMessage = message,
            };

            return Task.FromResult(failureContext);
        }
    }
}
