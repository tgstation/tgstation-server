namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public class User : Internal.User
	{
		/// <summary>
		/// The name of the default admin user
		/// </summary>
		public static readonly string AdminName = "Admin";

		/// <summary>
		/// The default admin password
		/// </summary>
		public static readonly string DefaultAdminPassword = "ISolemlySwearToDeleteTheDataDirectory";

		/// <summary>
		/// The <see cref="User"/> who created this <see cref="User"/>
		/// </summary>
		public Internal.User? CreatedBy { get; set; }
	}
}