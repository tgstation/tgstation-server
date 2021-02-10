using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base <see langword="class"/> for repository models.
	/// </summary>
	public class RepositoryApiBase : RepositorySettings
	{
		/// <summary>
		/// The branch or tag HEAD points to.
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		public string? Reference { get; set; }
	}
}
