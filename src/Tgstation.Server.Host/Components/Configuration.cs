using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class Configuration : IConfiguration
	{
		const string CodeModificationsSubdirectory = "CodeModifications";
		const string EventScriptsSubdirectory = "EventScripts";
		const string GameStaticFilesSubdirectory = "GameStaticFiles";

		const string CodeModificationsHeadFile = "HeadInclude.dm";
		const string CodeModificationsTailFile = "TailInclude.dm";

		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="Configuration"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="Configuration"/>
		/// </summary>
		readonly ILogger<Configuration> logger;

		/// <summary>
		/// Construct <see cref="Configuration"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Configuration(IIOManager ioManager, ILogger<Configuration> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<ServerSideModifications> CopyDMFilesTo(string destination, CancellationToken cancellationToken)
		{
			//just assume no other fs race conditions here
			var dmeExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, dmeFile), cancellationToken);
			var headFileExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, CodeModificationsHeadFile), cancellationToken);
			var tailFileExistsTask = ioManager.FileExists(ioManager.ConcatPath(CodeModificationsSubdirectory, CodeModificationsTailFile), cancellationToken);

			await Task.WhenAll(dmeExistsTask, headFileExistsTask, tailFileExistsTask).ConfigureAwait(false);

			if (!dmeExistsTask.Result && !headFileExistsTask.Result && !tailFileExistsTask.Result)
				return null;

			var copyTask = ioManager.CopyDirectory(CodeModificationsSubdirectory, destination, null, cancellationToken);

			if (dmeExistsTask.Result)
			{
				await copyTask.ConfigureAwait(false);
				return new ServerSideModifications(null, null, true);
			}

			if (!headFileExistsTask.Result && !tailFileExistsTask.Result)
			{
				await copyTask.ConfigureAwait(false);
				return null;
			}

			string IncludeLine(string filePath) => String.Format(CultureInfo.InvariantCulture, "#include \"{0}\"", filePath);

			await copyTask.ConfigureAwait(false);
			return new ServerSideModifications(headFileExistsTask.Result ? IncludeLine(CodeModificationsHeadFile) : null, tailFileExistsTask.Result ? IncludeLine(CodeModificationsTailFile) : null, false);
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<ConfigurationFile>> ListDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<ConfigurationFile> Read(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task SymlinkStaticFilesTo(string destination, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<string> Write(string configurationRelativePath, ISystemIdentity systemIdentity, byte[] data, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
