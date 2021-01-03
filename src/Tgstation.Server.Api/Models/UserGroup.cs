using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class UserGroup : Internal.UserGroup
	{
		/// <summary>
		/// The <see cref="User"/>s the <see cref="UserGroup"/> has.
		/// </summary>
		public ICollection<Internal.UserBase>? Users { get; set; }
	}
}
