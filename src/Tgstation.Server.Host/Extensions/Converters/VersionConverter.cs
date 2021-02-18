using Newtonsoft.Json;
using System;
using Tgstation.Server.Api;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Tgstation.Server.Host.Extensions.Converters
{
	/// <summary>
	/// <see cref="JsonConverter"/> and <see cref="IYamlTypeConverter"/> for serializing <see cref="global::System.Version"/>s in semver format.
	/// </summary>
	sealed class VersionConverter : JsonConverter, IYamlTypeConverter
	{
		/// <summary>
		/// Check if the <see cref="VersionConverter"/> supports (de)serializing a given <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to check.</param>
		/// <param name="validate">If the method should <see langword="throw"/> if validation fails.</param>
		/// <returns><see langword="true"/> if <paramref name="type"/> is a <see cref="global::System.Version"/>, <see langword="false"/> otherwise.</returns>
		static bool CheckSupportsType(Type type, bool validate)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			var supported = type == typeof(global::System.Version);
			if (!supported && validate)
				throw new NotSupportedException($"{nameof(VersionConverter)} does not convert {type}s!");

			return supported;
		}

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
			}
			else if (value is global::System.Version version)
			{
				writer.WriteValue(version.Semver().ToString());
			}
			else
			{
				throw new ArgumentException("Expected Version object!", nameof(value));
			}
		}

		/// <inheritdoc />
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			CheckSupportsType(objectType, true);

			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType == JsonToken.String)
			{
				try
				{
					var v = global::System.Version.Parse((string)reader.Value);
					return v.Semver();
				}
				catch (Exception ex)
				{
					throw new JsonSerializationException($"Error parsing version string: {reader.Value}", ex);
				}
			}

			throw new JsonSerializationException(
				$"Unexpected token or value when parsing version. Token: {reader.TokenType}, Value: {reader.Value}");
		}

		/// <inheritdoc />
		public override bool CanConvert(Type objectType) => CheckSupportsType(objectType, false);

		/// <inheritdoc />
		public bool Accepts(Type type) => CheckSupportsType(type, false);

		/// <inheritdoc />
		public object ReadYaml(IParser parser, Type type) => throw new NotSupportedException("Deserialization not supported!"); // The default implementation is fine at handling this

		/// <inheritdoc />
		public void WriteYaml(IEmitter emitter, object value, Type type)
		{
			if (emitter == null)
				throw new ArgumentNullException(nameof(emitter));

			CheckSupportsType(type, true);

			if (value == null)
				throw new NotSupportedException("Null values not supported!");

			var version = (global::System.Version)value;
			emitter.Emit(
				new Scalar(
					version
						.Semver()
						.ToString()));
		}
	}
}
