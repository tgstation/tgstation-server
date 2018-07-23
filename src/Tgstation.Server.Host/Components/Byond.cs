using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class Byond : IByond
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="Byond"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="Byond"/>
		/// </summary>
		readonly ILogger<Byond> logger;

		/// <summary>
		/// List of installed BYOND <see cref="Version"/>s
		/// </summary>
		readonly List<Version> installedVersions;

		/// <summary>
		/// Construct <see cref="Byond"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Byond(IIOManager ioManager, ILogger<Byond> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public Task ChangeVersion(Version version, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task ClearCache(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<Version> GetVersion(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public IByondExecutableLock UseExecutables(Version requiredVersion)
		{
			throw new NotImplementedException();
		}
	}
}
