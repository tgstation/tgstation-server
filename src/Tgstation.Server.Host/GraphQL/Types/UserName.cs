using System.Diagnostics.CodeAnalysis;

using Tgstation.Server.Host.GraphQL.Interfaces;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A <see cref="User"/> with limited fields.
	/// </summary>
	public class UserName : NamedEntity, IUserName
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserName"/> class.
		/// </summary>
		/// <param name="user">The <see cref="User"/> to copy.</param>
		[SetsRequiredMembers]
		public UserName(User user)
			: base(user)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserName"/> class.
		/// </summary>
		public UserName()
		{
		}
	}
}
