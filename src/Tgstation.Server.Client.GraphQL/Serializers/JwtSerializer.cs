using System;

using Microsoft.IdentityModel.JsonWebTokens;

using StrawberryShake.Serialization;

#pragma warning disable CA1812 // not detecting usage via annotation in schema.extensions.graphql

namespace Tgstation.Server.Client.GraphQL.Serializers
{
	/// <summary>
	/// <see cref="ScalarSerializer{TSerialized, TRuntime}"/> for <see cref="JsonWebToken"/>s.
	/// </summary>
	sealed class JwtSerializer : ScalarSerializer<string, JsonWebToken>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JwtSerializer"/> class.
		/// </summary>
		public JwtSerializer()
			: base("Jwt")
		{
		}

		/// <inheritdoc />
		public override JsonWebToken Parse(string serializedValue)
			=> new(serializedValue ?? throw new ArgumentNullException(nameof(serializedValue)));

		/// <inheritdoc />
		protected override string Format(JsonWebToken runtimeValue)
		{
			ArgumentNullException.ThrowIfNull(runtimeValue);
			return runtimeValue.EncodedToken;
		}
	}
}
