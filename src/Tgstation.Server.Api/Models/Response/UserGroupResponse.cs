using System.Collections.Generic;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <inheritdoc />
	public sealed class UserGroupResponse : UserGroup
	{
		/// <summary>
		/// The <see cref="UserName"/>s the <see cref="UserGroupResponse"/> has.
		/// </summary>
		public ICollection<UserName>? Users { get; set; }
	}
}
