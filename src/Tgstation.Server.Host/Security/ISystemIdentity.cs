using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Represents a user on the current <see cref="global::System.Runtime.InteropServices.OSPlatform"/>.
	/// </summary>
	public interface ISystemIdentity : IDisposable
	{
		/// <summary>
		/// A unique identifier for the user.
		/// </summary>
		string Uid { get; }

		/// <summary>
		/// The user's name.
		/// </summary>
		string Username { get; }

		/// <summary>
		/// If this system identity has permissions to create symlinks.
		/// </summary>
		bool CanCreateSymlinks { get; }

		/// <summary>
		/// Is this identity a SuperUser for the OS.
		/// See Administrator on Windows or root on Linux.
		/// </summary>
		bool IsSuperUser { get; }

		/// <summary>
		/// Clone the <see cref="ISystemIdentity"/> creating another copy that must have <see cref="IDisposable.Dispose"/> called on it.
		/// </summary>
		/// <returns>A new <see cref="ISystemIdentity"/> mirroring the current one.</returns>
		ISystemIdentity Clone();

		/// <summary>
		/// Runs a given <paramref name="action"/> in the context of the <see cref="ISystemIdentity"/>.
		/// </summary>
		/// <param name="action">The <see cref="Action"/> to perform, should be simple and not use any <see cref="Task"/>s or threading.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task RunImpersonated(Action action, CancellationToken cancellationToken);
	}
}
