using System;

using StrawberryShake.Serialization;

#pragma warning disable CA1812 // not detecting service provider usage

namespace Tgstation.Server.Client.GraphQL.Serializers
{
	/// <summary>
	/// <see cref="ScalarSerializer{TSerialized, TRuntime}"/> for <see cref="UInt32"/>s.
	/// </summary>
	sealed class UnsignedIntSerializer : ScalarSerializer<uint>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnsignedIntSerializer"/> class.
		/// </summary>
		public UnsignedIntSerializer()
			: base("UnsignedInt")
		{
		}
	}
}
