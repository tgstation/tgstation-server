using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	sealed class DefaultLogin : IDefaultLogin
	{
		/// <inheritdoc />
		public string UserName { get; } = User.AdminName;

		/// <inheritdoc />
		public string Password { get; set; } = User.DefaultAdminPassword;
	}
}
