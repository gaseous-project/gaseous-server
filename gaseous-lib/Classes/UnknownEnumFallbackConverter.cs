using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gaseous_server.Classes
{
    /// <summary>
    /// A Newtonsoft.Json converter that gracefully handles unrecognised enum values during
    /// deserialization. When an unknown string or numeric value is encountered the converter
    /// looks for a member named <c>Unknown</c> on the target enum (case-insensitive) and
    /// returns that. If no such member exists it falls back to the first declared member,
    /// or the default value for the type.
    ///
    /// This converter is safe to use with any enum type or nullable enum type.
    /// </summary>
    public sealed class UnknownEnumFallbackConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
            return enumType.IsEnum;
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
            bool isNullable = Nullable.GetUnderlyingType(objectType) != null;

            if (reader.TokenType == JsonToken.Null)
            {
                return isNullable ? null : GetUnknownValue(enumType);
            }

            JToken token = JToken.Load(reader);
            object? parsedValue = TryParseEnumValue(token, enumType);
            if (parsedValue != null)
            {
                return parsedValue;
            }

            object unknownValue = GetUnknownValue(enumType);
            Logging.LogKey(Logging.LogType.Warning, "UnknownEnumFallbackConverter",
                $"Unknown enum value \"{token}\" for enum \"{enumType.Name}\". Using \"{unknownValue}\".");
            return unknownValue;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.ToString());
        }

        private static object? TryParseEnumValue(JToken token, Type enumType)
        {
            if (token.Type == JTokenType.String)
            {
                string? stringValue = token.ToObject<string>();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                string? matchingName = Enum.GetNames(enumType)
                    .FirstOrDefault(name => string.Equals(name, stringValue, StringComparison.OrdinalIgnoreCase));
                if (matchingName != null)
                {
                    return Enum.Parse(enumType, matchingName);
                }

                if (long.TryParse(stringValue, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out long numericStringValue))
                {
                    return TryParseNumericEnumValue(enumType, numericStringValue);
                }

                return null;
            }

            if (token.Type == JTokenType.Integer)
            {
                long numericValue = token.ToObject<long>();
                return TryParseNumericEnumValue(enumType, numericValue);
            }

            return null;
        }

        private static object? TryParseNumericEnumValue(Type enumType, long numericValue)
        {
            object candidateValue = Enum.ToObject(enumType, numericValue);
            return Enum.IsDefined(enumType, candidateValue) ? candidateValue : null;
        }

        /// <summary>
        /// Returns the <c>Unknown</c> member of <paramref name="enumType"/> if one exists,
        /// otherwise the first declared member, or a default-constructed value as a last resort.
        /// </summary>
        private static object GetUnknownValue(Type enumType)
        {
            string? unknownName = Enum.GetNames(enumType)
                .FirstOrDefault(name => string.Equals(name, "Unknown", StringComparison.OrdinalIgnoreCase));
            if (unknownName != null)
            {
                return Enum.Parse(enumType, unknownName);
            }

            Array values = Enum.GetValues(enumType);
            return values.Length > 0 ? values.GetValue(0)! : Activator.CreateInstance(enumType)!;
        }
    }
}
