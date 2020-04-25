namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base class for <see cref="Models.ServerInformation"/>.
	/// </summary>
	public abstract class ServerInformation
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
		/// The maximum number of <see cref="Models.User"/>s allowed.
		/// </summary>
		public uint UserLimit { get; set; }
	}
}
