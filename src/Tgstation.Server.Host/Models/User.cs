using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a <see cref="Api.Models.User"/> in the database
	/// </summary>
	sealed class User : Api.Models.User
	{
		public string PasswordHash { get; set; }

		public string PasswordSalt { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s for the <see cref="User"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }
	}
}
