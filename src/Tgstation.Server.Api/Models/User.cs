namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a server <see cref="User"/>
	/// </summary>
	public sealed class User : Internal.User
	{
		[Permissions(DenyWrite = true)]
		public bool Enabled { get; set; }
	}
}