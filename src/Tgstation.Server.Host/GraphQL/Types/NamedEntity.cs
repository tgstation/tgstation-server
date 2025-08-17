using System;
using System.Diagnostics.CodeAnalysis;

using Tgstation.Server.Host.GraphQL.Interfaces;

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
		public required string Name { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NamedEntity"/> class.
		/// </summary>
		protected NamedEntity()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NamedEntity"/> class.
		/// </summary>
		/// <param name="copy">The <see cref="IUserName"/> to copy.</param>
		[SetsRequiredMembers]
		protected NamedEntity(NamedEntity copy)
			: base(copy?.Id ?? throw new ArgumentNullException(nameof(copy)))
		{
			Name = copy.Name;
		}
	}
}
