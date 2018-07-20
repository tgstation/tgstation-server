using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class SystemIdentityFactory : ISystemIdentityFactory
	{
		/// <inheritdoc />
		public Task<ISystemIdentity> CreateSystemIdentity(User user, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<ISystemIdentity> CreateSystemIdentity(string username, string password, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
