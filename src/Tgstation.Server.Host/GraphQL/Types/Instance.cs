using System;
using System.Linq;

namespace Tgstation.Server.Host.GraphQL.Types
{
	public sealed class Instance : Entity
	{
		public IQueryable<InstancePermissionSet> QueryableInstancePermissionSets()
			=> throw new NotImplementedException();
	}
}
