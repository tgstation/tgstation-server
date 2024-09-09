using System;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// An <see cref="Entity"/> with a <see cref="Name"/>.
	/// </summary>
	public abstract class NamedEntity : Entity
	{
		/// <summary>
		/// The name of the <see cref="NamedEntity"/>.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NamedEntity"/> class.
		/// </summary>
		/// <param name="id">The ID for the <see cref="Entity"/>.</param>
		/// <param name="name">The value of <see cref="Name"/>.</param>
		protected NamedEntity(long id, string name)
			: base(id)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}
	}
}
