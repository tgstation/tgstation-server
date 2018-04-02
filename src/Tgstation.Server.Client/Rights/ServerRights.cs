using System;

namespace Tgstation.Server.Client.Rights
{
	/// <summary>
	/// Rights a <see cref="IServerClient"/> has for a server. If a <see cref="IServerClient"/> method isn't listed below
	/// </summary>
	[Flags]
	public enum ServerRights
	{
		/// <summary>
		/// Client has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// Allow access to <see cref="IServerClient.GetServerVersion"/>
		/// </summary>
		/// <remarks>This implies access to any <see cref="IServerClient"/> APIs not otherwise listed here</remarks>
		Version = 1,
		/// <summary>
		/// Allow access to <see cref="IServerClient.Token"/>
		/// </summary>
		Tokens = 2,
	}
}