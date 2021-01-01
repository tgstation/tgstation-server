using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base class for <see cref="UserGroup"/>.
	/// </summary>
	public abstract class UserGroupBase : EntityId
	{
		/// <summary>
		/// The name of the <see cref="UserGroup"/>.
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? Name { get; set; }
	}
}
