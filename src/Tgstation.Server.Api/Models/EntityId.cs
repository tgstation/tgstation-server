namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Common base of entities with IDs.
	/// </summary>
	public class EntityId
	{
		/// <summary>
		/// The ID of the entity.
		/// </summary>
		/// <example>1</example>
		[RequestOptions(FieldPresence.Required)]
		[RequestOptions(FieldPresence.Ignored, PutOnly = true)]
		public virtual long? Id { get; set; }
	}
}
