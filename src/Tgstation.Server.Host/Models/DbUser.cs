using System.Collections.Generic;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a <see cref="Api.Models.User"/> in the database
	/// </summary>
	sealed class DbUser : Api.Models.User
	{
		public string PasswordHash { get; set; }

		public string PasswordSalt { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s for the <see cref="DbUser"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="Token"/>s for the <see cref="DbUser"/>
		/// </summary>
		public List<Token> Tokens { get; set; }
	}
}
