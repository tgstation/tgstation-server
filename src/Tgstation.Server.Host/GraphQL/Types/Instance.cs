using System;
using System.Linq;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a game server instance.
	/// </summary>
	public sealed class Instance : Entity
	{
		/// <summary>
		/// Queries all <see cref="InstancePermissionSet"/>s in the <see cref="Instance"/>.
		/// </summary>
		/// <returns>Queryable <see cref="InstancePermissionSet"/>s.</returns>
		public IQueryable<InstancePermissionSet> QueryableInstancePermissionSets()
			=> throw new NotImplementedException();

		/// <summary>
		/// Gets the callers effective <see cref="InstancePermissionSet"/> on the <see cref="Instance"/>.
		/// </summary>
		/// <returns>The callers effective <see cref="InstancePermissionSet"/> if it exists or <see langword="null"/>.</returns>
		public InstancePermissionSet? EffectivePermissionSet()
			=> throw new NotImplementedException();
	}
}
