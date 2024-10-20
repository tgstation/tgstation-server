using System;

using HotChocolate.Language;
using HotChocolate.Types;
using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Host.GraphQL.Scalars
{
	/// <summary>
	/// A <see cref="ScalarType{TRuntimeType, TLiteral}"/> for semantic <see cref="Version"/>s.
	/// </summary>
	public sealed class SemverType : ScalarType<Version, StringValueNode>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SemverType"/> class.
		/// </summary>
		public SemverType()
			: base("Semver")
		{
			Description = "Represents a version in semantic versioning format";
			SpecifiedBy = new Uri("https://semver.org/spec/v2.0.0.html");
		}

		/// <inheritdoc />
		public override IValueNode ParseResult(object? resultValue)
			=> ParseValue(resultValue);

		/// <inheritdoc />
		public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
		{
			if (resultValue is not string resultString)
			{
				runtimeValue = null;
				return false;
			}

			var result = Version.TryParse(resultString, out var resultVersion);
			runtimeValue = resultVersion;
			return result;
		}

		/// <inheritdoc />
		public override bool TrySerialize(object? runtimeValue, out object? resultValue)
		{
			if (runtimeValue is not Version runtimeVersion)
			{
				resultValue = null;
				return false;
			}

			resultValue = runtimeVersion.Semver().ToString();
			return true;
		}

		/// <inheritdoc />
		protected override Version ParseLiteral(StringValueNode valueSyntax)
		{
			ArgumentNullException.ThrowIfNull(valueSyntax);
			return Version.Parse(valueSyntax.Value);
		}

		/// <inheritdoc />
		protected override StringValueNode ParseValue(Version runtimeValue)
			=> new(runtimeValue.Semver().ToString());

		/// <inheritdoc />
		protected override bool IsInstanceOfType(StringValueNode valueSyntax)
		{
			ArgumentNullException.ThrowIfNull(valueSyntax);
			return Version.TryParse(valueSyntax.Value, out var parsedVersion)
				&& IsInstanceOfType(parsedVersion);
		}

		/// <inheritdoc />
		protected override bool IsInstanceOfType(Version runtimeValue)
		{
			ArgumentNullException.ThrowIfNull(runtimeValue);
			return runtimeValue.Build != -1 && runtimeValue.Revision == -1;
		}
	}
}
