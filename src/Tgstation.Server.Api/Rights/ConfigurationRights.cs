using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.IConfigurationFile"/>s.
	/// </summary>
	[Flags]
	public enum ConfigurationRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User may read files if the <see cref="Models.Instance"/> allows it.
		/// </summary>
		Read = 1 << 0,

		/// <summary>
		/// User may write files if the <see cref="Models.Instance"/> allows it.
		/// </summary>
		Write = 1 << 1,

		/// <summary>
		/// User may list files if the <see cref="Models.Instance"/> allows it.
		/// </summary>
		List = 1 << 2,

		/// <summary>
		/// User may delete empty folders if the <see cref="Models.Instance"/> allows it.
		/// </summary>
		Delete = 1 << 3,
	}
}
