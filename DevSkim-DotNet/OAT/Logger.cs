// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT License.
using Serilog;
using Serilog.Events;

namespace Microsoft.CST.OAT.Utils
{
    /// <summary>
    /// This is the logger class for OAT
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Should Debug Logging be enabled
        /// </summary>
        public static bool Debug { get; set; }
        /// <summary>
        /// Should Quiet logging be enabled (Warn and above)
        /// </summary>
        public static bool Quiet { get; set; }
        /// <summary>
        /// Should Verbose logging be enabled
        /// </summary>
        public static bool Verbose { get; set; }

        /// <summary>
        /// Setup Quiet Logging
        /// </summary>
        public static void SetupQuiet()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Console()
                .CreateLogger();
        }

        /// <summary>
        /// Set up Verbose Logging
        /// </summary>
        public static void SetupVerbose()
        {
            Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Console()
                    .CreateLogger();
        }

        /// <summary>
        /// Set up Debug Logging
        /// </summary>
        public static void SetupDebug()
        {
            Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .CreateLogger();
        }

        /// <summary>
        /// Set up Information Logging
        /// </summary>
        public static void SetupInformation()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Information()
               .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
               .CreateLogger();
        }
    }
}