using Tgstation.Server.Host.GraphQL.Interfaces;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A <see cref="User"/> with limited fields.
	/// </summary>
	public sealed class UserName : NamedEntity, IUserName
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserName"/> class.
		/// </summary>
		/// <param name="copy">The <see cref="NamedEntity"/> to copy.</param>
		public UserName(NamedEntity copy)
			: base(copy)
		{
		}
	}
}
