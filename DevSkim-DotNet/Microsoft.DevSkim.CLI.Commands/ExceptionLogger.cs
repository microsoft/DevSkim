// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.DevSkim.CLI
{
    public class ExceptionLogger
    {
        public ExceptionLogger()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        public string Write(Exception ex)
        {
            string message = "Message: " + ex.Message + "\r\n";
            message += "StackTrace: " + ex.StackTrace;

            return WriteRaw(message) ?? string.Empty;
        }

        public string? WriteRaw(string? rawData)
        {
            string? fileName = "devskim_exception.log";
            FileStream? fs = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    fs = null;
                    sw.WriteLine("Date:                " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    sw.WriteLine("Platform:            " + Environment.OSVersion.VersionString);
                    sw.WriteLine("Processor count:     " + Environment.ProcessorCount);
                    sw.WriteLine("Memory:              " + Environment.WorkingSet);
                    sw.WriteLine("Culture:             " + CultureInfo.CurrentCulture.Name);
                    if (rawData != null)
                        sw.WriteLine(rawData);
                    else
                        sw.WriteLine("NULL argument was passed to exception logger");
                    sw.WriteLine(new string('-', 70));
                    sw.Flush();
                }
            }
            catch
            {
                fileName = null;
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }

            return fileName;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((Exception)e.ExceptionObject);
        }

        private void LogException(Exception e)
        {
            string fileName = Write(e);

            Console.Error.WriteLine("Critical exception happend. Details dumped into {0}", fileName);
            Environment.Exit((int)ExitCode.CriticalError);
        }
    }
}