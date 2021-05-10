namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// The type of configuration allowed on an <see cref="Instance"/>.
	/// </summary>
	public enum ConfigurationType
	{
		/// <summary>
		/// Configuration editing is not allowed.
		/// </summary>
		Disallowed,

		/// <summary>
		/// Configuration editing is allowed by all users on all files.
		/// </summary>
		HostWrite,

		/// <summary>
		/// Configuration editing is allowed by only by system identity users and uses their filesystem ACLs.
		/// </summary>
		SystemIdentityWrite,
	}
}
