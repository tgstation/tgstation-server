namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public class User : Internal.User
	{
		/// <summary>
		/// The name of the default admin user
		/// </summary>
		public const string AdminName = "Admin";

		/// <summary>
		/// The default admin password
		/// </summary>
		public const string DefaultAdminPassword = "ISolemlySwearToDeleteTheDataDirectory";

		/// <summary>
		/// The <see cref="User"/> who created this <see cref="User"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public User CreatedBy { get; set; }
	}
}