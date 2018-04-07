using System.Collections.Generic;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a <see cref="Api.Models.User"/> in the database
	/// </summary>
	sealed class User : Api.Models.User
	{
		/// <summary>
		/// The <see cref="InstanceUser"/>s for the <see cref="User"/>
		/// </summary>
		List<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="Token"/>s for the <see cref="User"/>
		/// </summary>
		List<Token> Tokens { get; set; }
	}
}
