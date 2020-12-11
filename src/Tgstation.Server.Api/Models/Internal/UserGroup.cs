using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a group of <see cref="Models.User"/>s.
	/// </summary>
	public class UserGroup : EntityId
	{
		/// <summary>
		/// The name of the <see cref="UserGroup"/>.
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? Name { get; set; }
	}
}
