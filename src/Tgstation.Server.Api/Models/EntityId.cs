namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Common base of <see cref="Instance"/>s, <see cref="CompileJob"/>s, and <see cref="Job"/>s.
	/// </summary>
	public class EntityId
	{
		/// <summary>
		/// The ID of the entity.
		/// </summary>
		public long Id { get; set; }
	}
}
