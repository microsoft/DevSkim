// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.DevSkim
{
    /// <summary>
    /// Issue severity
    /// </summary>
    [Flags]
    public enum Severity 
    {        
        Critical = 1,
        Important = 2,
        Moderate = 4,
        Low = 8,
        Informational = 16,
        DefenseInDepth = 32,
        ManualReview = 64
    }

    /// <summary>
    /// Json Converter for Severity
    /// </summary>
    class SeverityConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Severity svr = (Severity)value;
            string svrstr = svr.ToString().ToLower();

            switch (svr)
            {
                case Severity.DefenseInDepth:
                    svrstr = "defense-in-depth";
                    break;
                case Severity.ManualReview:
                    svrstr = "manual-review";
                    break;
            }

            writer.WriteValue(svrstr);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            enumString = enumString.Replace("-", "");
            return Enum.Parse(typeof(Severity), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}