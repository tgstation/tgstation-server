using System;

using HotChocolate.Language;
using HotChocolate.Types;

namespace Tgstation.Server.Host.GraphQL.Scalars
{
	/// <summary>
	/// A <see cref="ScalarType{TRuntimeType, TLiteral}"/> for specialized <see cref="string"/> types.
	/// </summary>
	public abstract class StringScalarType : ScalarType<string, StringValueNode>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringScalarType"/> class.
		/// </summary>
		/// <param name="name">The name of the GraphQL scalar type.</param>
		public StringScalarType(string name)
			: base(name)
		{
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
