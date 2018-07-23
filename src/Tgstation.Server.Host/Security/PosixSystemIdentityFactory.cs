using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentityFactory"/> for posix systems
	/// </summary>
	sealed class PosixSystemIdentityFactory : ISystemIdentityFactory
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
