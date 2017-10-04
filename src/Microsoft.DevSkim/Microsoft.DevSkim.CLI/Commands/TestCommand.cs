using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class TestCommand : ICommand
    {
        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Run tests for rules";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");


            command.OnExecute(() => {
                return (new TestCommand(locationArgument.Value)).Run();
            });
        }

        public TestCommand(string path)
        {
            _path = path;
        }

        public int Run()
        {
            if (!Directory.Exists(_path))
            {
                Console.Error.WriteLine("Error: Not a valid file or directory {0}", _path);
                return 1;
            }

            Compiler compiler = new Compiler(_path);
            if (!compiler.Compile())
                return 1;

            Tester tester = new Tester(compiler.CompiledRuleset);
            tester.Run(_path);

            return 0;
        }

        private string _path;
    }
}