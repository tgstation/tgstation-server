using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// For editing a given user.
	/// </summary>
	public class UserUpdateRequest : UserApiBase
	{
		/// <summary>
		/// Cleartext password of the user.
		/// </summary>
		[Required]
		public string? Password { get; set; }
	}
}
