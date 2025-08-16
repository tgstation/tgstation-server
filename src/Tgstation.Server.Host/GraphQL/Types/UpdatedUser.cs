using HotChocolate.Types.Relay;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a <see cref="User"/> that has been updated.
	/// </summary>
	public sealed class UpdatedUser
	{
		/// <summary>
		/// The <see cref="User"/>'s <see cref="Entity.Id"/>.
		/// </summary>
		[ID(nameof(Types.User))]
		public required long Id { get; init; }

		/// <summary>
		/// The <see cref="Types.User"/>, if was authorized to be read.
		/// </summary>
		public required User? User { get; set; }
	}
}
