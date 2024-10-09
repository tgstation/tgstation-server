using HotChocolate.Types.Relay;

namespace Tgstation.Server.Host.GraphQL.Interfaces
{
	/// <summary>
	/// A lightly scoped <see cref="Types.User"/>.
	/// </summary>
	public interface IUserName
	{
		/// <summary>
		/// The ID of the user.
		/// </summary>
		[ID]
		public long Id { get; }

		/// <summary>
		/// The name of the user.
		/// </summary>
		public string Name { get; }
	}
}
