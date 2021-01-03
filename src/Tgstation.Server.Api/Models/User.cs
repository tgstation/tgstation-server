using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
		[Required]
		public Internal.UserBase? CreatedBy { get; set; }

		/// <summary>
		/// List of <see cref="OAuthConnection"/>s associated with the <see cref="User"/>.
		/// </summary>
		public ICollection<OAuthConnection>? OAuthConnections { get; set; }

		/// <summary>
		/// The <see cref="Models.PermissionSet"/> directly associated with the <see cref="User"/>.
		/// </summary>
		public PermissionSet? PermissionSet { get; set; }

		/// <summary>
		/// The <see cref="Internal.UserGroup"/> asociated with the <see cref="User"/>, if any.
		/// </summary>
		public Internal.UserGroup? Group { get; set; }
	}
}
