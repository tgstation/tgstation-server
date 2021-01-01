using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base class for <see cref="User"/>.
	/// </summary>
	public class UserBase
	{
		/// <summary>
		/// The ID of the <see cref="User"/>
		/// </summary>
		[Required]
		public long? Id { get; set; }

		/// <summary>
		/// The name of the <see cref="User"/>
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? Name { get; set; }
	}
}
