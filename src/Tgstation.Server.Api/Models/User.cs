namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public class User : Internal.User
	{
		/// <summary>
		/// The <see cref="User"/> who created this <see cref="User"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public User CreatedBy { get; set; }
	}
}