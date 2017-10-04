using Microsoft.Extensions.CommandLineUtils;
using System.Collections.Generic;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class CatalogueCommand : ICommand
    {
        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Catalogue rules into a csv file";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");

            var outputArgument = command.Argument("[output]",
                                                    "Output file");

            var columnsOptions = command.Option("-c|--column",
                                              "Column in catalogue",
                                              CommandOptionType.MultipleValue);

            command.OnExecute(() => {
                return (new CatalogueCommand(locationArgument.Value,
                                 outputArgument.Value,
                                 columnsOptions.Values)).Run();                
            });
        }

        public CatalogueCommand(string path, string output, List<string> properties)
        {
            _path = path;
            _outputfile = output;
            _columns = properties.ToArray();
        }

        public int Run()
        {
            Compiler compiler = new Compiler(_path);

            if (!compiler.Compile())
                return 1;
            
            Catalogue catalogue = new Catalogue(compiler.CompiledRuleset);
            catalogue.ToCsv(_outputfile, _columns);

            return 0;
        }

        private string _path;
        private string _outputfile;
        private string[] _columns;
    }
}