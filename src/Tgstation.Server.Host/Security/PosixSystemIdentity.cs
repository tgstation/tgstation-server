using System;
using System.Threading;
using System.Threading.Tasks;

using Mono.Unix.Native;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentity"/> for POSIX systems.
	/// </summary>
	sealed class PosixSystemIdentity : ISystemIdentity
	{
		/// <summary>
		/// True if TGS is running under root.
		/// </summary>
		bool isRoot = false;

		/// <summary>
		/// True if <see cref="isRoot" /> is populated.
		/// </summary>
		bool isRootChecked = false;

		/// <summary>
		/// Checks whether TGS is running under the root user.
		/// </summary>
		/// <returns>True if running under root. False otherwise.</returns>
		public bool IsRoot()
		{
			if (isRootChecked)
			{
				return isRoot;
			}

			isRoot = Syscall.getuid() == 0;
			isRootChecked = true;
			return isRoot;
		}

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
