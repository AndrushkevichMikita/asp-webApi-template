using ApiTemplate.SharedKernel.PrimitivesExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiTemplate.SharedKernel.Extensions
{
    public static class JsonDefaults
    {
        public static JsonSerializerOptions Defaults { get; } = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // instead of obsolete IgnoreNullValues = true in .net 5
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new IsoDateTimeConverter() }
        };

        public static IMvcBuilder AddJsonDefaults(this IMvcBuilder builder, Action<JsonOptions> configure = null)
        {
            builder
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReadCommentHandling = Defaults.ReadCommentHandling;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = Defaults.DefaultIgnoreCondition;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = Defaults.PropertyNameCaseInsensitive;
                    foreach (var c in Defaults.Converters)
                    {
                        options.JsonSerializerOptions.Converters.Add(c);
                    }
                });

            if (configure != null)
                builder.AddJsonOptions(configure);

            return builder;
        }
    }

    /** Helper to set DateTime in UTC into UI-local (without Z) so UI can work properly */
    public class DateUtcAsLocalConveter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DateTime v, JsonSerializerOptions options)
        {
            writer.WriteStringValue(v.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"));
        }
    }
}
