using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
		/// The <see cref="ISynchronousIOManager"/> for <see cref="Configuration"/>
		/// </summary>
		readonly ISynchronousIOManager synchronousIOManager;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="Configuration"/>
		/// </summary>
		readonly ILogger<Configuration> logger;

		/// <summary>
		/// Construct <see cref="Configuration"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="synchronousIOManager">The value of <see cref="synchronousIOManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Configuration(IIOManager ioManager, ISynchronousIOManager synchronousIOManager, ILogger<Configuration> logger)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.synchronousIOManager = synchronousIOManager ?? throw new ArgumentNullException(nameof(synchronousIOManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async Task<ServerSideModifications> CopyDMFilesTo(string dmeFile, string destination, CancellationToken cancellationToken)
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

		string ValidateConfigRelativePath(string configurationRelativePath)
		{
			if (String.IsNullOrEmpty(configurationRelativePath))
				configurationRelativePath = ".";
			return ioManager.ResolvePath(configurationRelativePath);
		}

		/// <inheritdoc />
		public async Task<IReadOnlyList<ConfigurationFile>> ListDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			var path = ValidateConfigRelativePath(configurationRelativePath);

			List<ConfigurationFile> result = new List<ConfigurationFile>();

			void ListImpl()
			{
				var enumerator = synchronousIOManager.GetDirectories(configurationRelativePath, cancellationToken);
				result.AddRange(enumerator.Select(x => new ConfigurationFile
				{
					IsDirectory = true,
					Path = ioManager.ConcatPath(configurationRelativePath, x),
				}));
				enumerator = synchronousIOManager.GetFiles(configurationRelativePath, cancellationToken);
				result.AddRange(enumerator.Select(x => new ConfigurationFile
				{
					IsDirectory = false,
					Path = ioManager.ConcatPath(configurationRelativePath, x),
				}));
			}

			if (systemIdentity == null)
				ListImpl();
			else
				await systemIdentity.RunImpersonated(ListImpl, cancellationToken).ConfigureAwait(false);

			return result;
		}

		/// <inheritdoc />
		public async Task<ConfigurationFile> Read(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken)
		{
			var path = ValidateConfigRelativePath(configurationRelativePath);

			ConfigurationFile result = null;
			
			void ReadImpl()
			{
				try
				{
					var content = synchronousIOManager.ReadFile(path);
					string sha1String;
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.
					using (var sha1 = new SHA1Managed())
#pragma warning restore CA5350 // Do not use insecure cryptographic algorithm SHA1.
						sha1String = String.Join("", sha1.ComputeHash(content).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
					result = new ConfigurationFile
					{
						Content = content,
						IsDirectory = false,
						LastReadHash = sha1String,
						AccessDenied = false,
						Path = configurationRelativePath
					};
				}
				catch (FileNotFoundException) { }
				catch (DirectoryNotFoundException) { }
				catch (UnauthorizedAccessException)
				{
					result = new ConfigurationFile
					{
						AccessDenied = true,
						Path = configurationRelativePath
					};
				}
			}

			if (systemIdentity == null)
				ReadImpl();
			else
				await systemIdentity.RunImpersonated(ReadImpl, cancellationToken).ConfigureAwait(false);

			return result;
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
