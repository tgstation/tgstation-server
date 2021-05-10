using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// <see cref="ISystemIdentityFactory"/> for windows systems. Uses long running tasks due to potential networked domains.
	/// </summary>
	sealed class WindowsSystemIdentityFactory : ISystemIdentityFactory
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="WindowsSystemIdentityFactory"/>.
		/// </summary>
		readonly ILogger<WindowsSystemIdentityFactory> logger;

		/// <summary>
		/// Extract the username and domain name from a <see cref="string"/> in the format "username\\domainname".
		/// </summary>
		/// <param name="input">The input <see cref="string"/>.</param>
		/// <param name="username">The output username.</param>
		/// <param name="domainName">The output domain name. May be <see langword="null"/>.</param>
		static void GetUserAndDomainName(string input, out string username, out string domainName)
		{
			var splits = input.Split('\\');
			username = splits.Length > 1 ? splits[1] : splits[0];
			domainName = splits.Length > 1 ? splits[0] : null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsSystemIdentityFactory"/> class.
		/// </summary>
		/// <param name="logger">The value of logger.</param>
		public WindowsSystemIdentityFactory(ILogger<WindowsSystemIdentityFactory> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public ISystemIdentity GetCurrent() => new WindowsSystemIdentity(WindowsIdentity.GetCurrent());

		/// <inheritdoc />
		public Task<ISystemIdentity> CreateSystemIdentity(User user, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				if (user == null)
					throw new ArgumentNullException(nameof(user));

				if (user.SystemIdentifier == null)
					throw new InvalidOperationException("User's SystemIdentifier must not be null!");

				PrincipalContext pc = null;
				UserPrincipal principal = null;

				bool TryGetPrincipalFromContextType(ContextType contextType)
				{
					try
					{
						pc = new PrincipalContext(contextType);
						cancellationToken.ThrowIfCancellationRequested();
						principal = UserPrincipal.FindByIdentity(pc, user.SystemIdentifier);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception e)
					{
						logger.LogWarning(e, "Error loading user for context type {0}!", contextType);
					}
					finally
					{
						if (principal == null)
						{
							pc?.Dispose();
							cancellationToken.ThrowIfCancellationRequested();
						}
					}

					return principal != null;
				}

				if (!TryGetPrincipalFromContextType(ContextType.Machine) && !TryGetPrincipalFromContextType(ContextType.Domain))
					return null;
				return (ISystemIdentity)new WindowsSystemIdentity(principal);
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<ISystemIdentity> CreateSystemIdentity(string username, string password, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				if (username == null)
					throw new ArgumentNullException(nameof(username));
				if (password == null)
					throw new ArgumentNullException(nameof(password));

				var originalUsername = username;
				GetUserAndDomainName(originalUsername, out username, out var domainName);

				var res = NativeMethods.LogonUser(username, domainName, password, 3 /*LOGON32_LOGON_NETWORK*/, 0 /*LOGON32_PROVIDER_DEFAULT*/, out var token);
				if (!res)
				{
					logger.LogTrace("Invalid system identity/password combo for username {0}!", originalUsername);
					return null;
				}

				logger.LogTrace("Authenticated username {0} using system identity!", originalUsername);

				// checked internally, windows identity always duplicates the handle when constructed
				using var handle = new SafeAccessTokenHandle(token);
				return (ISystemIdentity)new WindowsSystemIdentity(
					new WindowsIdentity(handle.DangerousGetHandle()));   // https://github.com/dotnet/corefx/blob/6ed61acebe3214fcf79b4274f2bb9b55c0604a4d/src/System.Security.Principal.Windows/src/System/Security/Principal/WindowsIdentity.cs#L271
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);
	}
}
