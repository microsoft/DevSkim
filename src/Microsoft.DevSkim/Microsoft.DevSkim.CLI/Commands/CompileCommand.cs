using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class CompileCommand : ICommand
   {
        public static void Configure(CommandLineApplication command)
        {

            command.Description = "Compile and verify integrity of rules";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");

            command.OnExecute(() => {
                return (new CompileCommand(locationArgument.Value)).Run();                
            });
        }

        public CompileCommand(string path)
        {
            _path = path;
        }

        public int Run()
        {            
            Compiler compiler = new Compiler(_path);
            if (compiler.Compile())
            {
                Console.Error.WriteLine("No errors found.");
                return 1;
            }

            return 0;
        }

        private string _path;
    }
}
