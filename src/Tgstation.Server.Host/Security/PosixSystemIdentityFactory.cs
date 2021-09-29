using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentityFactory"/> for posix systems.
	/// </summary>
	/// <remarks>TODO: Blocked by https://github.com/dotnet/corefx/issues/3187 (Abandoned)</remarks>
	[UnsupportedOSPlatform("windows")]
	sealed class PosixSystemIdentityFactory : ISystemIdentityFactory
	{
		/// <inheritdoc />
		public ISystemIdentity GetCurrent() => new PosixSystemIdentity();

		/// <inheritdoc />
		public Task<ISystemIdentity?> CreateSystemIdentity(User user, CancellationToken cancellationToken) => throw new NotImplementedException();

		/// <inheritdoc />
		public Task<ISystemIdentity?> CreateSystemIdentity(string username, string password, CancellationToken cancellationToken) => throw new NotImplementedException();
	}
}
