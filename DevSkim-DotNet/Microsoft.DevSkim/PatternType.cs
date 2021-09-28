// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System;
using System.Text.Json.Serialization;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Pattern Type for search pattern
    /// </summary>
    [JsonConverter(typeof(PatternTypeConverter))]

    public enum PatternType
    {
        Regex,
        RegexWord,
        String,
        Substring
    }

    /// <summary>
    ///     Json converter for Pattern Type
    /// </summary>
    internal class PatternTypeConverter : JsonConverter<PatternType>
    {
        public override PatternType Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.GetString() is string value)
            {
                if (Enum.TryParse<PatternType>(value.Replace("-", ""), true, out PatternType result))
                {
                    return result;
                }
            }
            return 0;
        }

        public override void Write(
            Utf8JsonWriter writer,
            PatternType patternTypeValue,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(patternTypeValue.ToString());
    }
}