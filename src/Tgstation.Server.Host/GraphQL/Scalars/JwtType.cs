using System;

namespace Tgstation.Server.Host.GraphQL.Scalars
{
	/// <summary>
	/// A <see cref="StringScalarType"/> for encoded JSON Web Tokens.
	/// </summary>
	public sealed class JwtType : StringScalarType
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
	}
}
