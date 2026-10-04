using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clawfoot.ResultPattern
{
    /// <summary>
    /// System.Text.Json converter for <see cref="IError.Kind"/>. Writes a kind as <c>"TypeName.Member"</c>
    /// (e.g. <c>"AppErrorKind.NotFound"</c>) and reads it back into the registered enum type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writing works for any enum without setup: <see cref="Error.Kind"/> is annotated with this converter.
    /// </para>
    /// <para>
    /// Reading needs to know which enum types a name can refer to. <see cref="ErrorKind"/> is always known; register
    /// your own once on the options:
    /// <code>options.Converters.Add(new ErrorKindJsonConverter(typeof(AppErrorKind)));</code>
    /// The registered converter is used for <see cref="Error.Kind"/> and for any other property declared as <see cref="Enum"/>,
    /// such as the kind on a custom <see cref="IError"/> implementation.
    /// Register a single converter listing all your kind types: only the first one in <c>options.Converters</c> is used.
    /// </para>
    /// <para>
    /// A kind that can't be matched to a registered type and member throws <see cref="JsonException"/>, so a missing
    /// registration fails on first use rather than silently losing the kind. Pass <c>ignoreUnknownKinds: true</c> to read
    /// unknown kinds as null instead, e.g. in a client that may receive kinds added by a newer server.
    /// </para>
    /// </remarks>
    public sealed class ErrorKindJsonConverter : JsonConverter<Enum>
    {
        private readonly Dictionary<string, Type> _kindTypes;
        private readonly bool _ignoreUnknownKinds;

        /// <summary>
        /// Create a converter that only knows the built-in <see cref="ErrorKind"/>.
        /// </summary>
        public ErrorKindJsonConverter()
            : this(false)
        { }

        /// <summary>
        /// Create a converter that reads the given enum types, plus the built-in <see cref="ErrorKind"/>.
        /// </summary>
        /// <param name="kindTypes">The application's error kind enums</param>
        public ErrorKindJsonConverter(params Type[] kindTypes)
            : this(false, kindTypes)
        { }

        /// <summary>
        /// Create a converter that reads the given enum types, plus the built-in <see cref="ErrorKind"/>.
        /// </summary>
        /// <param name="ignoreUnknownKinds">
        /// True to read kinds that can't be matched as null. False (the default) throws <see cref="JsonException"/>.
        /// </param>
        /// <param name="kindTypes">The application's error kind enums</param>
        /// <exception cref="ArgumentException">A type isn't an enum, or two types share the same name</exception>
        public ErrorKindJsonConverter(bool ignoreUnknownKinds, params Type[] kindTypes)
        {
            _ignoreUnknownKinds = ignoreUnknownKinds;
            _kindTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (var type in new[] { typeof(ErrorKind) }.Concat(kindTypes ?? Array.Empty<Type>()))
            {
                if (type is null || !type.IsEnum)
                    throw new ArgumentException($"Error kinds must be enum types, but got {type?.FullName ?? "null"}", nameof(kindTypes));

                if (_kindTypes.TryGetValue(type.Name, out var existing))
                {
                    if (existing == type) continue;
                    throw new ArgumentException(
                        $"Error kinds are serialized by type name, so {existing.FullName} and {type.FullName} can't both be registered",
                        nameof(kindTypes));
                }

                _kindTypes.Add(type.Name, type);
            }
        }

        /// <inheritdoc/>
        public override Enum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected an error kind string such as \"ErrorKind.NotFound\", but got {reader.TokenType}");

            // [JsonConverter] on a property outranks options.Converters, so the instance created for Error.Kind
            // hands off to the one the application registered, which knows its kind types.
            var resolver = options.Converters.OfType<ErrorKindJsonConverter>().FirstOrDefault() ?? this;
            return resolver.Parse(reader.GetString()!);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.GetType().Name}.{value}");
        }

        private Enum? Parse(string text)
        {
            var separator = text.LastIndexOf('.');
            if (separator <= 0 || separator == text.Length - 1)
                throw new JsonException($"\"{text}\" is not an error kind: expected \"TypeName.Member\", such as \"ErrorKind.NotFound\"");

            var typeName = text.Substring(0, separator);
            var member = text.Substring(separator + 1);

            if (!_kindTypes.TryGetValue(typeName, out var type))
            {
                if (_ignoreUnknownKinds) return null;
                throw new JsonException(
                    $"Unknown error kind \"{text}\": the enum type {typeName} isn't registered. " +
                    $"Register it with options.Converters.Add(new {nameof(ErrorKindJsonConverter)}(typeof({typeName}))).");
            }

            if (!Enum.TryParse(type, member, false, out var value))
            {
                if (_ignoreUnknownKinds) return null;
                throw new JsonException($"Unknown error kind \"{text}\": {type.FullName} has no member {member}.");
            }

            return (Enum)value!;
        }
    }
}
