using System;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
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
	[SupportedOSPlatform("windows")]
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
		static void GetUserAndDomainName(string input, out string username, out string? domainName)
		{
			var splits = input.Split('\\');
			username = splits.Length > 1 ? splits[1] : splits[0];
			domainName = splits.Length > 1 ? splits[0] : null;
		}

		/// <summary>
		/// Try and get a <paramref name="userPrincipal"/> from a given <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The <see cref="User"/>.</param>
		/// <param name="contextType">The <see cref="ContextType"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <param name="principalContext">The resulting <see cref="PrincipalContext"/>.</param>
		/// <param name="userPrincipal">The resulting <see cref="UserPrincipal"/>.</param>
		/// <returns><see langword="true"/> if <paramref name="userPrincipal"/> was loaded, <see langword="false"/> otherwise.</returns>
		bool TryGetPrincipalFromContextType(
			User user,
			ContextType contextType,
			CancellationToken cancellationToken,
			[NotNullWhen(true)] out PrincipalContext? principalContext,
			[NotNullWhen(true)] out UserPrincipal? userPrincipal)
		{
			userPrincipal = null;
			principalContext = null;
			try
			{
				principalContext = new PrincipalContext(contextType);
				cancellationToken.ThrowIfCancellationRequested();
				userPrincipal = UserPrincipal.FindByIdentity(principalContext, user.SystemIdentifier);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogWarning(e, "Error loading user for context type {contextType}!", contextType);
			}
			finally
			{
				if (userPrincipal == null)
				{
					principalContext?.Dispose();
					principalContext = null;
					cancellationToken.ThrowIfCancellationRequested();
				}
			}

			return userPrincipal != null;
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
		public Task<ISystemIdentity?> CreateSystemIdentity(User user, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				if (user == null)
					throw new ArgumentNullException(nameof(user));

				if (user.SystemIdentifier == null)
					throw new InvalidOperationException("User's SystemIdentifier must not be null!");

				if (!TryGetPrincipalFromContextType(
					user,
					ContextType.Machine,
					cancellationToken,
					out _,
					out var userPrincipal)
					&& !TryGetPrincipalFromContextType(
					user,
					ContextType.Domain,
					cancellationToken,
					out _,
					out userPrincipal))
					return null;
				return (ISystemIdentity)new WindowsSystemIdentity(userPrincipal);
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<ISystemIdentity?> CreateSystemIdentity(string username, string password, CancellationToken cancellationToken) => Task.Factory.StartNew(
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
					logger.LogTrace("Invalid system identity/password combo for username {originalUsername}!", originalUsername);
					return null;
				}

				logger.LogTrace("Authenticated username {originalUsername} using system identity!", originalUsername);

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
