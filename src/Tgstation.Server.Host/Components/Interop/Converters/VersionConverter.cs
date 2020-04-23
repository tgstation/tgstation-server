using Newtonsoft.Json;
using System;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Components.Interop.Converters
{
	/// <summary>
	/// <see cref="JsonConverter"/> for serializing <see cref="Version"/>s for BYOND.
	/// </summary>
	sealed class VersionConverter : JsonConverter
	{
		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
			}
			else if (value is Version version)
			{
				writer.WriteValue(version.Semver());
			}
			else
			{
				throw new JsonSerializationException("Expected Version object value!");
			}
		}

		/// <inheritdoc />
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType == JsonToken.String)
			{
				try
				{
					Version v = new Version((string)reader.Value);
					return v;
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
		public override bool CanConvert(Type objectType) => objectType == typeof(Version);
	}
}
