using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentity"/> for windows systems
	/// </summary>
	sealed class WindowsSystemIdentity : ISystemIdentity
	{
		/// <summary>
		/// The <see cref="WindowsIdentity"/> for the <see cref="WindowsSystemIdentity"/>
		/// </summary>
		readonly WindowsIdentity identity;

		/// <summary>
		/// Construct a <see cref="WindowsSystemIdentity"/>
		/// </summary>
		/// <param name="identity">The value of <see cref="identity"/></param>
		public WindowsSystemIdentity(WindowsIdentity identity)
		{
			this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
		}

		/// <inheritdoc />
		public void Dispose() => identity.Dispose();

		/// <inheritdoc />
		public string Uid => identity.User.ToString();

		/// <inheritdoc />
		public string Username => identity.Name;

		/// <inheritdoc />
		public ISystemIdentity Clone() => new WindowsSystemIdentity((WindowsIdentity)identity.Clone());

		/// <inheritdoc />
		public Task RunImpersonated(Action action, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			WindowsIdentity.RunImpersonated(identity.AccessToken, action);
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}