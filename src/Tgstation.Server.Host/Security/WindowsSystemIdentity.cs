using System;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentity"/> for windows systems.
	/// </summary>
	[SupportedOSPlatform("windows")]
	sealed class WindowsSystemIdentity : ISystemIdentity
	{
		/// <inheritdoc />
		public string Uid => (userPrincipal?.Sid ?? identity!.User!).ToString(); // we kno user isn't null because it can only be the case when anonymous (checked in this constructor)

		/// <inheritdoc />
		public string Username => userPrincipal?.Name ?? identity!.Name;

		/// <inheritdoc />
		public bool CanCreateSymlinks => IsSuperUser;

		/// <inheritdoc />
		public bool IsSuperUser => isAdmin ?? throw new NotSupportedException();

		/// <summary>
		/// The <see cref="WindowsIdentity"/> for the <see cref="WindowsSystemIdentity"/>.
		/// </summary>
		readonly WindowsIdentity? identity;

		/// <summary>
		/// The <see cref="UserPrincipal"/> for the <see cref="WindowsSystemIdentity"/>.
		/// </summary>
		readonly UserPrincipal? userPrincipal;

		/// <summary>
		/// Backing field for <see cref="IsSuperUser"/>.
		/// </summary>
		readonly bool? isAdmin;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsSystemIdentity"/> class.
		/// </summary>
		/// <param name="identity">The value of <see cref="identity"/>.</param>
		public WindowsSystemIdentity(WindowsIdentity identity)
		{
			this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
			if (identity.IsAnonymous)
				throw new ArgumentException($"Cannot use anonymous {nameof(WindowsIdentity)} as a {nameof(WindowsSystemIdentity)}!", nameof(identity));

			isAdmin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsSystemIdentity"/> class.
		/// </summary>
		/// <param name="userPrincipal">The value of <see cref="userPrincipal"/>.</param>
		public WindowsSystemIdentity(UserPrincipal userPrincipal)
		{
			this.userPrincipal = userPrincipal ?? throw new ArgumentNullException(nameof(userPrincipal));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (identity != null)
				identity.Dispose();
			else
			{
				var context = userPrincipal!.Context;
				userPrincipal.Dispose();
				context.Dispose();
			}
		}

		/// <inheritdoc />
		public ISystemIdentity Clone()
		{
			if (identity != null)
			{
				// var newIdentity = (WindowsIdentity)identity.Clone(); //doesn't work because of https://github.com/dotnet/corefx/issues/31841
				var newIdentity = new WindowsIdentity(identity.Token); // the handle is cloned internally

				return new WindowsSystemIdentity(newIdentity);
			}

			// can't clone a UP, shouldn't be trying to anyway, cloning is for impersonation
			throw new NotSupportedException("Cannot clone a UserPrincipal based WindowsSystemIdentity!");
		}

		/// <inheritdoc />
		public Task RunImpersonated(Action action, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				ArgumentNullException.ThrowIfNull(action);
				if (identity == null)
					throw new NotSupportedException("Impersonate using a UserPrincipal based WindowsSystemIdentity!");
				WindowsIdentity.RunImpersonated(identity.AccessToken, action);
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);
	}
}
