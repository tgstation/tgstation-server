using System;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class SystemIdentityFactory : ISystemIdentityFactory
	{
		/// <inheritdoc />
		public ISystemIdentity CreateSystemIdentity(User user)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public ISystemIdentity CreateSystemIdentity(string username, string password)
		{
			throw new NotImplementedException();
		}
	}
}
