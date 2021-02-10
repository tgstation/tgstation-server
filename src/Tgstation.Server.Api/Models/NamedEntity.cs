using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Base class for named entities.
	/// </summary>
	public class NamedEntity : EntityId
	{
		/// <summary>
		/// The name of the entity represented by the <see cref="NamedEntity"/>.
		/// </summary>
		[Required]
		[ResponseOptions]
		[RequestOptions(FieldPresence.Required, PutOnly = true)]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? Name { get; set; }
	}
}
