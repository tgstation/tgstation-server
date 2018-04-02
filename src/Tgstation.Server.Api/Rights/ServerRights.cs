using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights a user has for an entire server
	/// </summary>
	[Flags]
	public enum ServerRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// Allow access to the <see cref="Version"/> of the server
		/// </summary>
		Version = 1,
		/// <summary>
		/// Allow access to <see cref="Models.Token"/>s
		/// </summary>
		Tokens = 2,
	}
}