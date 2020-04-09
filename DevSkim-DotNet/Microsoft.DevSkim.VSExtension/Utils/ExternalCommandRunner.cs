using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.DevSkim.VSExtension.Utils
{
    public static class ExternalCommandRunner
    {
        public static int RunExternalCommand(string command, params string[] args) => RunExternalCommand(command, out _, out _, string.Join(" ", args), true);

        public static int RunExternalCommand(string command, out string stdOut, out string stdError, params string[] args) => RunExternalCommand(command, out stdOut, out stdError, string.Join(" ", args), true);

        public static int RunExternalCommand(string filename, out string stdOut, out string stdError, string arguments = "", bool Redirect = true, string workingDirectory = "")
        {
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = string.IsNullOrEmpty(arguments) ? string.Empty : arguments,
                    RedirectStandardOutput = Redirect,
                    RedirectStandardError = Redirect,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = workingDirectory
                }
            })
            {
                var stdOutput = new StringBuilder();
                process.OutputDataReceived += (sender, args) => stdOutput.AppendLine(args.Data); // Use AppendLine rather than Append since args.Data is one line of output, not including the newline character.

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    throw new ExternalException("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
                }

                stdOut = stdOutput.ToString();

                return process.ExitCode;
            }
        }

        private static string Format(string filename, string arguments)
        {
            return "'" + filename +
                ((string.IsNullOrEmpty(arguments)) ? string.Empty : " " + arguments) +
                "'";
        }


    }
}