using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class ConfigurationClient : IConfigurationClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="ConfigurationClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ConfigurationClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="ConfigurationClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public ConfigurationClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task DeleteEmptyDirectory(ConfigurationFile directory, CancellationToken cancellationToken) => apiClient.Delete(Routes.Configuration, directory, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<ConfigurationFile> CreateDirectory(ConfigurationFile directory, CancellationToken cancellationToken) => apiClient.Create<ConfigurationFile, ConfigurationFile>(Routes.Configuration, directory, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<ConfigurationFile>> List(string directory, CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<ConfigurationFile>>(Routes.ListRoute(Routes.Configuration) + Routes.SanitizeGetPath(directory), instance.Id, cancellationToken);

		/// <inheritdoc />
		public async Task<Tuple<ConfigurationFile, Stream>> Read(ConfigurationFile file, CancellationToken cancellationToken)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			var configFile = await apiClient.Read<ConfigurationFile>(
				Routes.ConfigurationFile + Routes.SanitizeGetPath(file.Path ?? throw new ArgumentException("file.Path should not be null!", nameof(file))),
				instance.Id,
				cancellationToken)
				.ConfigureAwait(false);
			var downloadStream = await apiClient.Download(configFile, cancellationToken).ConfigureAwait(false);
			try
			{
				return Tuple.Create(configFile, downloadStream);
			}
			catch
			{
				downloadStream.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async Task<ConfigurationFile> Write(ConfigurationFile file, Stream uploadStream, CancellationToken cancellationToken)
		{
			MemoryStream? memoryStream = null;
			if (uploadStream != null)
				memoryStream = new MemoryStream();

			using (memoryStream)
			{
				var configFileTask = apiClient.Update<ConfigurationFile, ConfigurationFile>(
					Routes.Configuration,
					file ?? throw new ArgumentNullException(nameof(file)),
					instance.Id,
					cancellationToken);

				if (uploadStream != null)
					await uploadStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

				var configFile = await configFileTask.ConfigureAwait(false);

				// minor improvement to "fix" a lost feature that used to be in API 7
				// since LastReadHash is no longer updated until the next GET request, we can use the same calculations here to generate it.
				if (uploadStream != null)
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.
					using (var sha1 = new SHA1Managed())
#pragma warning restore CA5350 // Do not use insecure cryptographic algorithm SHA1.
						configFile.LastReadHash = String.Join(String.Empty, sha1.ComputeHash(memoryStream).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
				else
					configFile.LastReadHash = null;

				await apiClient.Upload(configFile, memoryStream, cancellationToken).ConfigureAwait(false);

				return configFile;
			}
		}
	}
}
