using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.Byond"/>
	/// </summary>
	[Flags]
	public enum ByondRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User may check the active installed BYOND version
		/// </summary>
		ReadActive = 1,
		/// <summary>
		/// User may change to any BYOND version
		/// </summary>
		ChangeVersion = 4
	}
}
