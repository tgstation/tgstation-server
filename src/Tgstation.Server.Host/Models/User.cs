using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	sealed class User : Api.Models.Internal.User
	{
		/// <summary>
		/// The hash of the user's password
		/// </summary>
		public string PasswordHash { get; set; }

		/// <summary>
		/// The value used to encrypt JWTs
		/// </summary>
		[StringLength(CryptographySuite.SecureStringLength)]
		public string TokenSecret { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.User"/>
		/// </summary>
		public User CreatedBy { get; set; }

		/// <summary>
		/// <see cref="User"/>s created by this <see cref="User"/>
		/// </summary>
		public List<User> CreatedUsers { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s for the <see cref="User"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }
	}
}
