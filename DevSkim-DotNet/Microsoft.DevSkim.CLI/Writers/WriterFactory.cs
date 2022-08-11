// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

namespace Microsoft.DevSkim.CLI.Writers
{
    public class WriterFactory
    {
        public static Writer GetWriter(string writerName, string format, TextWriter output, string? outputPath)
        {
            if (string.IsNullOrEmpty(writerName))
                writerName = "_dummy";

            switch (writerName.ToLowerInvariant())
            {
                case "_dummy":
                    return new DummyWriter();

                case "json":
                    return new JsonWriter(format, output);

                case "text":
                    return new SimpleTextWriter(format, output);

                case "sarif":
                    return new SarifWriter(output, outputPath);

                default:
                    throw new Exception("wrong output");
            }
        }
    }
}