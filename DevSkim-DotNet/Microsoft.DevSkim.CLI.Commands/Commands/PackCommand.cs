// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Microsoft.Extensions.CommandLineUtils;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace Microsoft.DevSkim.CLI.Commands
{
    public class PackCommand : ICommand
    {
        public PackCommand(string path, string output, bool indent, bool checkGuidanceOnline = false)
        {
            _path = path;
            _outputfile = output;
            _indent = indent;
            _checkGuidance = checkGuidanceOnline;
        }

        public static void Configure(CommandLineApplication command)
        {
            command.Description = "Pack rules into a single file";
            command.HelpOption("-?|-h|--help");

            var locationArgument = command.Argument("[path]",
                                                    "Path to rules");

            var outputArgument = command.Argument("[output]",
                                                    "Output file");

            var indentOption = command.Option("-i|--indent",
                                              "Indent the output json",
                                              CommandOptionType.NoValue);
            var checkGuidance = command.Option("-c", "Check online to see if guidance documents referenced exist.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                return (new PackCommand(locationArgument.Value,
                                 outputArgument.Value,
                                 indentOption.HasValue(),
                                 checkGuidance.HasValue())).Run();
            });
        }

        public int Run()
        {
            Verifier verifier = new Verifier(_path);

            if (!verifier.Verify(_checkGuidance))
            {
                foreach(var message in verifier.Messages)
                {
                    Console.WriteLine($"{message.File} - {message.Message}");
                }
                return (int)ExitCode.IssuesExists;
            }

            List<Rule> list = new List<Rule>(verifier.CompiledRuleset.AsEnumerable().Select(x => x.DevSkimRule));

            var netOptions = new JsonSerializerOptions();
            netOptions.WriteIndented = _indent;
            netOptions.Converters.Add(new JsonStringEnumConverter());
            netOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;

            using FileStream fs = File.Open(_outputfile, FileMode.Create, FileAccess.Write);
            using StreamWriter sw = new StreamWriter(fs);
            var serialized = JsonSerializer.Serialize(list, netOptions);
            sw.Write(serialized);
            sw.Close();
            fs.Close();

            return (int)ExitCode.NoIssues;
        }

        private bool _indent;
        private bool _checkGuidance;
        private string _outputfile;
        private string _path;
    }
}