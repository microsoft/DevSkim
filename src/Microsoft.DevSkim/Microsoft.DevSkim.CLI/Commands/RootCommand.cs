using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class RootCommand : ICommand
    {
        public static void Configure(CommandLineApplication app)
        {
            app.Name = "devskim";
            app.HelpOption("-?|-h|--help");

            app.Command("analyze", AnalyzeCommand.Configure);
            app.Command("verify", VerifyCommand.Configure);
            app.Command("pack", PackCommand.Configure);
            app.Command("catalogue", CatalogueCommand.Configure);
            app.Command("test", TestCommand.Configure);

            app.OnExecute(() => {
                return (new RootCommand(app)).Run();                
            });
        }

        public RootCommand(CommandLineApplication app)
        {
            _app = app;
        }

        public int Run()
        {
            _app.ShowHelp();
            return 0;
        }

        CommandLineApplication _app;
    }
}