using System.Collections.Generic;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base class for <see cref="Response.ServerInformationResponse"/>.
	/// </summary>
	public abstract class ServerInformationBase
	{
		/// <summary>
		/// Minimum length of database user passwords.
		/// </summary>
		/// <example>20</example>
		public uint MinimumPasswordLength { get; set; }

		/// <summary>
		/// The maximum number of <see cref="Instance"/>s allowed.
		/// </summary>
		/// <example>100</example>
		public uint InstanceLimit { get; set; }

		/// <summary>
		/// The maximum number of users allowed.
		/// </summary>
		/// <example>100</example>
		public uint UserLimit { get; set; }

		/// <summary>
		/// The maximum number of user groups allowed.
		/// </summary>
		/// <example>50</example>
		public uint UserGroupLimit { get; set; }

		/// <summary>
		/// Limits the locations instances may be created or attached from.
		/// </summary>
		/// <example>["/home/tgstation-server/my-server-1", "/home/tgstation-server/my-server-2"]</example>
		[ResponseOptions]
		public List<string>? ValidInstancePaths { get; set; }
	}
}
