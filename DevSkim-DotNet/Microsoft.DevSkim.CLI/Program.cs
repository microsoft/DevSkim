// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.DevSkim.CLI.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.DevSkim.CLI
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            RootCommand.Configure(app);
            return app.Execute(args);
        }
    }
}