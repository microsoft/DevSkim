// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.DevSkim.CLI.Writers
{
    public class WriterFactory
    {
        public static Writer GetWriter(string writerName, string defaultWritter, string format = null)
        {
            if (string.IsNullOrEmpty(writerName))
                writerName = defaultWritter;
            
            if (string.IsNullOrEmpty(writerName))
                writerName = "_dummy";

            switch (writerName.ToLowerInvariant())
            {  
                case "_dummy":
                    return new DummyWriter();
                case "json":
                    return new JsonWriter(format);                    
                case "text":
                    return new SimpleTextWriter(format);
                case "sarif":
                    return new SarifWriter();
                default:
                    throw new Exception("wrong output");
            }
        }
    }
}
