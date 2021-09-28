// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System.Text.Json;
using System;
using System.Text.Json.Serialization;

namespace Microsoft.DevSkim
{
    /// <summary>
    ///     Issue severity
    /// </summary>
    [Flags]
    [JsonConverter(typeof(SeverityConverter))]
    public enum Severity
    {
        /// <summary>
        ///     Critial issues
        /// </summary>
        Critical = 1,

        /// <summary>
        ///     Important issues
        /// </summary>
        Important = 2,

        /// <summary>
        ///     Moderate issues
        /// </summary>
        Moderate = 4,

        /// <summary>
        ///     Best Practice
        /// </summary>
        BestPractice = 8,

        /// <summary>
        ///     Issues that require manual review
        /// </summary>
        ManualReview = 16
    }

    /// <summary>
    ///     Json Converter for Severity
    /// </summary>
    internal class SeverityConverter : JsonConverter<Severity>
    {
        public override Severity Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.GetString() is string value)
                {
                    if (Enum.TryParse(value.Replace("-", ""), out Severity result))
                    {
                        return result;
                    }
                }
                return 0;
            }

        public override void Write(
            Utf8JsonWriter writer,
            Severity severityValue,
            JsonSerializerOptions options) =>
                writer.WriteStringValue(severityValue.ToString());
    }
}