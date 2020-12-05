using System;
using System.Collections.Generic;
using System.IO;
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
			long initialStreamPosition = 0;
			MemoryStream? memoryStream = null;
			if (uploadStream?.CanSeek == false)
				memoryStream = new MemoryStream();
			else if (uploadStream != null)
				initialStreamPosition = uploadStream.Position;

			using (memoryStream)
			{
				var configFileTask = apiClient.Update<ConfigurationFile, ConfigurationFile>(
					Routes.Configuration,
					file ?? throw new ArgumentNullException(nameof(file)),
					instance.Id,
					cancellationToken);

				if (memoryStream != null)
					await uploadStream!.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

				var configFile = await configFileTask.ConfigureAwait(false);

				var streamUsed = memoryStream ?? uploadStream;
				streamUsed?.Seek(initialStreamPosition, SeekOrigin.Begin);
				await apiClient.Upload(configFile, streamUsed, cancellationToken).ConfigureAwait(false);

				return configFile;
			}
		}
	}
}
