using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelpersCommon.PrimitivesExtensions
{
    public class IsoDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
            writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ"));
        }
    }
}
