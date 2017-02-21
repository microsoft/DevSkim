// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace DevSkim
{
    public enum PatternType
    {
        Regex,
        Regex_Word,
        String,
        Substring
    }

    /// <summary>
    /// Json converter for Pattern Type
    /// </summary>
    class PatternTypeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            PatternType svr = (PatternType)value;
            writer.WriteValue(svr.ToString().ToLower());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;

            return Enum.Parse(typeof(PatternType), enumString, true);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}
