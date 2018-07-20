using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// For editing a given <see cref="User"/>. Will never be returned by the API
	/// </summary>
    public sealed class UserUpdate : User
	{
		/// <summary>
		/// Cleartext password of the <see cref="User"/>
		/// </summary>
		[Permissions(WriteRight = AdministrationRights.EditUsers)]
		public string Password { get; set; }
    }
}
