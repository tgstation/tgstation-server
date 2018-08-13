using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.ConfigurationFile"/>
	/// </summary>
	[Flags]
	public enum ConfigurationRights : ulong
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User may read files
		/// </summary>
		Read = 1,
		/// <summary>
		/// User may write files
		/// </summary>
		Write = 2,
		/// <summary>
		/// User may list files
		/// </summary>
		List = 3
	}
}
