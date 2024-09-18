using System;

using HotChocolate.Language;
using HotChocolate.Types;

namespace Tgstation.Server.Host.GraphQL.Types.Scalars
{
	/// <summary>
	/// A <see cref="ScalarType{TRuntimeType, TLiteral}"/> for encoded JSON Web Tokens.
	/// </summary>
	public sealed class JwtType : ScalarType<string, StringValueNode>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JwtType"/> class.
		/// </summary>
		public JwtType()
			: base("Jwt")
		{
			Description = "Represents an encoded JSON Web Token";
			SpecifiedBy = new Uri("https://datatracker.ietf.org/doc/html/rfc7519");
		}

		/// <inheritdoc />
		public override IValueNode ParseResult(object? resultValue)
			=> ParseValue(resultValue);

		/// <inheritdoc />
		protected override string ParseLiteral(StringValueNode valueSyntax)
		{
			ArgumentNullException.ThrowIfNull(valueSyntax);
			return valueSyntax.Value;
		}

		/// <inheritdoc />
		protected override StringValueNode ParseValue(string runtimeValue)
			=> new(runtimeValue);
	}
}
