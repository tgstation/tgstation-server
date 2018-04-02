using System;

namespace Tgstation.Server.Client.Rights
{
	/// <summary>
	/// Rights for <see cref="Components.ITokenClient"/>
	/// </summary>
	[Flags]
    public enum TokenRights
    {
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User can create <see cref="Api.Models.Token"/>s
		/// </summary>
		Create = 1,
		/// <summary>
		/// User can delete <see cref="Api.Models.Token"/>s
		/// </summary>
		Delete = 2,
		/// <summary>
		/// User can list all their <see cref="Api.Models.TokenInfo"/>s
		/// </summary>
		List = 4,
		/// <summary>
		/// User can perform all the actions they can for themselves on behalf of other users except <see cref="Create"/>
		/// </summary>
		Admin = 8,
    }
}
