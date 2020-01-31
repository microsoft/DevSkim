// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.DevSkim.CLI.Commands;

namespace Microsoft.DevSkim.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            RootCommand.Configure(app);
            return app.Execute(args);
        }
    }
}
