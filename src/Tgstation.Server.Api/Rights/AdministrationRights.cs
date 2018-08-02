using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.Administration"/>
	/// </summary>
	[Flags]
	public enum AdministrationRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User can edit themself and other <see cref="Models.User"/>s
		/// </summary>
		EditUsers = 1,
		/// <summary>
		/// User can gracefully restart the host
		/// </summary>
		RestartHost = 2,
		/// <summary>
		/// User can change <see cref="Models.Administration.CurrentVersion"/>
		/// </summary>
		ChangeVersion = 4,
		/// <summary>
		/// User can change their password
		/// </summary>
		EditPassword = 8,
	}
}
