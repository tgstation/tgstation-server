using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public class UserResponse : UserApiBase
	{
		/// <summary>
		/// The <see cref="UserResponse"/> who created this <see cref="UserResponse"/>
		/// </summary>
		[Required]
		public NamedEntity? CreatedBy { get; set; }
	}
}
