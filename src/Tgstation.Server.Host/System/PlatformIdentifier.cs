using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class PlatformIdentifier : IPlatformIdentifier
	{
		/// <inheritdoc />
		public bool IsWindows { get; }

		/// <inheritdoc />
		public string ScriptFileExtension { get; }

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="PlatformIdentifier"/>.
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="IRepositoryFactory"/> for the <see cref="PlatformIdentifier"/>.
		/// </summary>
		readonly IRepositoryFactory repositoryFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PlatformIdentifier"/>.
		/// </summary>
		readonly ILogger<PlatformIdentifier> logger;

		/// <summary>
		/// Construct a <see cref="PlatformIdentifier"/>
		/// </summary>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="repositoryFactory">The value of <see cref="repositoryFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PlatformIdentifier(
			ISystemIdentityFactory systemIdentityFactory,
			IRepositoryFactory repositoryFactory,
			ILogger<PlatformIdentifier> logger)
		{
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			ScriptFileExtension = IsWindows ? "bat" : "sh";
		}

		/// <inheritdoc />
		public void CheckCompatibility()
		{
			try
			{
				repositoryFactory.CreateInMemory().Dispose();
			}
			catch
			{
				logger.LogCritical("Unable to initialize libgit2! This is a common problem on POSIX installations. Try using Docker.");
				throw;
			}

			using var systemIdentity = systemIdentityFactory.GetCurrent();
			if (!systemIdentity.CanCreateSymlinks)
				throw new InvalidOperationException("The user running tgstation-server cannot create symlinks! Please try running as an administrative user!");
		}
	}
}
