// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Code Fix Type
    /// </summary>
    public enum FixType
    {
        RegexReplace
    }

    /// <summary>
    ///     Json Converter for FixType
    /// </summary>
    internal class FixTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value is string enumString)
            {
                enumString = enumString.Replace("-", "");
                return Enum.Parse(typeof(FixType), enumString, true);
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is FixType svr)
            {
                string svrstr = svr.ToString().ToLower();

                switch (svr)
                {
                    case FixType.RegexReplace:
                        svrstr = "regex-replace";
                        break;
                }
                writer.WriteValue(svrstr);
            }
        }
    }
}