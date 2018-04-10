using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a <see cref="Api.Models.User"/> in the database
	/// </summary>
	sealed class User : Api.Models.Internal.User
	{
		public string PasswordHash { get; set; }

		/// <summary>
		/// The value used for the <see cref="SymmetricSecurityKey"/> to encrypt JWTs
		/// </summary>
		[StringLength(CryptographySuite.SecureStringLength)]
		public string TokenSecret { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s for the <see cref="User"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }
	}
}
