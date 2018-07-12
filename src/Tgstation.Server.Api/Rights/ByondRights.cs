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
		/// User may check the installed BYOND version
		/// </summary>
		ReadInstalled = 1,
		/// <summary>
		/// User may check the previous BYOND version
		/// </summary>
		ReadPrevious = 2,
		/// <summary>
		/// User may change to any BYOND version
		/// </summary>
		ChangeVersion = 4,
		/// <summary>
		/// User may cancel a pending installation job
		/// </summary>
		Cancel = 8,
		/// <summary>
		/// User may read the <see cref="Models.ByondStatus"/> of the installation job
		/// </summary>
		ReadStatus = 16,
	}
}
