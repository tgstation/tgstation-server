using System;

using Newtonsoft.Json;

namespace Tgstation.Server.Host.Extensions.Converters
{
	/// <summary>
	/// <see cref="JsonConverter"/> for decoding <see cref="bool"/>s returned by BYOND.
	/// </summary>
	sealed class BoolConverter : JsonConverter
	{
		/// <inheritdoc />
		public override void WriteJson(JsonWriter? writer, object? value, JsonSerializer serializer)
		{
			ArgumentNullException.ThrowIfNull(writer);
			writer.WriteValue(((bool)value!) ? 1 : 0);
		}

		/// <inheritdoc />
		public override object? ReadJson(JsonReader? reader, Type? objectType, object? existingValue, JsonSerializer serializer)
		{
			ArgumentNullException.ThrowIfNull(reader);
			return reader.Value!.ToString() == "1";
		}

		/// <inheritdoc />
		public override bool CanConvert(Type objectType) => objectType == typeof(bool);
	}
}
