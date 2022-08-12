// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.Extensions.CommandLineUtils;
using System.Reflection;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class RootCommand : ICommand
    {
        public RootCommand(CommandLineApplication app)
        {
            _app = app;
        }

        public static void Configure(CommandLineApplication app)
        {
            app.FullName = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyProductAttribute>()?
                               .Product;

            app.Name = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyTitleAttribute>()?
                               .Title;

            app.HelpOption("-?|-h|--help");
            app.VersionOption("-v|--version", Assembly.GetEntryAssembly()?
                                                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                      .InformationalVersion);

            app.Command("analyze", AnalyzeCommand.Configure, false);
            app.Command("verify", VerifyCommand.Configure, false);

            app.OnExecute(() =>
            {
                return (new RootCommand(app)).Run();
            });
        }

        public int Run()
        {
            _app.ShowHelp();

            return (int)ExitCode.NoIssues;
        }

        private CommandLineApplication _app;
    }
}