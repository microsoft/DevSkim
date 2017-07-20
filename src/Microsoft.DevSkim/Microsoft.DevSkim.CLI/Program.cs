using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DevSkim;

namespace Microsoft.DevSkim.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse Arguments
            args = new string[] { @"c:\dev\XFiles\test" };

            // For now, only argument is path to scan
            if (args.Length != 1)
            {
                ShowUsage();
                System.Environment.Exit(1);
            }

            // Path must exist
            var path = args[0];
            if (!Directory.Exists(path))
            {
                ShowUsage();
                System.Environment.Exit(1);
            }

            Ruleset rules = Ruleset.FromDirectory(@"..\..\..\..\..\rules", null);
            RuleProcessor processor = new RuleProcessor(rules);

            foreach (string filename in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var fileText = File.ReadAllText(filename);
                foreach (var issue in processor.Analyze(fileText, Language.FromFileName(filename)))
                {
                    Console.WriteLine("{0}:{1} {2}", filename, GetLineNumberFromOffset(fileText, issue.Index), issue.Rule.Name);
                }
            }
            Console.ReadKey();
        }

        static int GetLineNumberFromOffset(string text, int offset)
        {
            return text.Substring(0, offset).Count(s => s == '\n') + 1;
        }
        
        static void ShowUsage()
        {
            Console.Error.WriteLine("Usage: DevSkim.exe path-to-scan");
        }
        
    }
}
