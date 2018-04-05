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
		/// User can change <see cref="Models.Administration.SystemAuthenticationGroup"/>
		/// </summary>
		ChangeAuthenticationGroup = 1,
		/// <summary>
		/// User can change <see cref="Models.Administration.EnableTelemetry"/>
		/// </summary>
		ChangeTelemetry = 2,
		/// <summary>
		/// User can edit themself and other <see cref="Models.User"/>s
		/// </summary>
		EditUsers = 4,
		/// <summary>
		/// User can change <see cref="Models.Administration.SoftStop"/>
		/// </summary>
		SoftStop = 8,
		/// <summary>
		/// User can change <see cref="Models.Administration.CurrentVersion"/>
		/// </summary>
		ChangeVersion = 16,
		/// <summary>
		/// User can change <see cref="Models.Administration.UpstreamRepository"/>
		/// </summary>
		SetUpstreamRepository

	}
}
