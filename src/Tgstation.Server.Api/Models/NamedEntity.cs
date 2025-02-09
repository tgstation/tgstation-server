using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Base class for named entities.
	/// </summary>
	public abstract class NamedEntity : EntityId
	{
		/// <summary>
		/// The name of the entity represented by the <see cref="NamedEntity"/>.
		/// </summary>
		/// <example>MyThingyName</example>
		[Required]
		[RequestOptions(FieldPresence.Required, PutOnly = true)]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public virtual string? Name { get; set; }
	}
}
