using System.Collections.Generic;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base class for <see cref="Response.ServerInformationResponse"/>.
	/// </summary>
	public abstract class GatewayInformationBase
	{
		/// <summary>
		/// Minimum length of database user passwords.
		/// </summary>
		public uint MinimumPasswordLength { get; set; }

		/// <summary>
		/// The maximum number of <see cref="Instance"/>s allowed.
		/// </summary>
		public uint InstanceLimit { get; set; }

		/// <summary>
		/// The maximum number of users allowed.
		/// </summary>
		public uint UserLimit { get; set; }

		/// <summary>
		/// The maximum number of user groups allowed.
		/// </summary>
		public uint UserGroupLimit { get; set; }

		/// <summary>
		/// Limits the locations instances may be created or attached from.
		/// </summary>
		[ResponseOptions]
		public ICollection<string>? ValidInstancePaths { get; set; }
	}
}
