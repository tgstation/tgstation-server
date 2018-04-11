namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class User : Internal.User
	{
		/// <summary>
		/// If the <see cref="User"/> is enabled since users cannot be deleted
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool Enabled { get; set; }
	}
}