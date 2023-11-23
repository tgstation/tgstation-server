using System;
using System.Threading;
using System.Threading.Tasks;

#nullable disable

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentity"/> for POSIX systems.
	/// </summary>
	sealed class PosixSystemIdentity : ISystemIdentity
	{
		/// <inheritdoc />
		public string Uid => throw new NotImplementedException();

		/// <inheritdoc />
		public string Username => throw new NotImplementedException();

		/// <inheritdoc />
		public bool CanCreateSymlinks => true;

		/// <inheritdoc />
		public ISystemIdentity Clone() => throw new NotImplementedException();

		/// <inheritdoc />
		public void Dispose()
		{
		}

		/// <inheritdoc />
		public Task RunImpersonated(Action action, CancellationToken cancellationToken) => throw new NotSupportedException();
	}
}
