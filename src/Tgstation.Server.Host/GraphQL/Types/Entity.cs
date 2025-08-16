using System.Diagnostics.CodeAnalysis;

using HotChocolate.Types.Relay;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a database entity.
	/// </summary>
	public abstract class Entity
	{
		/// <summary>
		/// The ID of the <see cref="Entity"/>.
		/// </summary>
		[ID]
		public virtual required long Id { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Entity"/> class.
		/// </summary>
		protected Entity()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Entity"/> class.
		/// </summary>
		/// <param name="id">The value of <see cref="Id"/>.</param>
		[SetsRequiredMembers]
		protected Entity(long id)
		{
			Id = id;
		}
	}
}
