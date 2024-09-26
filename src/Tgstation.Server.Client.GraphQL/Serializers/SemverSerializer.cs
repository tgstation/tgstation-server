using System;

using StrawberryShake.Serialization;

using Tgstation.Server.Common.Extensions;

#pragma warning disable CA1812 // not detecting usage via annotation in schema.extensions.graphql

namespace Tgstation.Server.Client.GraphQL.Serializers
{
	/// <summary>
	/// <see cref="ScalarSerializer{TSerialized, TRuntime}"/> for <see cref="Version"/>s.
	/// </summary>
	sealed class SemverSerializer : ScalarSerializer<string, Version>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SemverSerializer"/> class.
		/// </summary>
		public SemverSerializer()
			: base("Semver")
		{
		}

		/// <inheritdoc />
		public override Version Parse(string serializedValue)
			=> Version.Parse(serializedValue ?? throw new ArgumentNullException(nameof(serializedValue)));

		/// <inheritdoc />
		protected override string Format(Version runtimeValue)
		{
			ArgumentNullException.ThrowIfNull(runtimeValue);
			return runtimeValue.Semver().ToString();
		}
	}
}
