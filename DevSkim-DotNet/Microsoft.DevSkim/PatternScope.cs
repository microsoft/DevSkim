// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System;
using System.Text.Json.Serialization;

namespace Microsoft.DevSkim
{
    [JsonConverter(typeof(PatternScopeConverter))]
    public enum PatternScope
    {
        All,
        Code,
        Comment,
        Html
    }

    /// <summary>
    ///     Json converter for Pattern Type
    /// </summary>
    internal class PatternScopeConverter : JsonConverter<PatternScope>
    {
        public override PatternScope Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.GetString() is string value)
            {
                if (Enum.TryParse<PatternScope>(value.Replace("-", ""), out PatternScope result))
                {
                    return result;
                }
            }
            return 0;
        }

        public override void Write(
            Utf8JsonWriter writer,
            PatternScope patternScopeValue,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(patternScopeValue.ToString());
    }
}