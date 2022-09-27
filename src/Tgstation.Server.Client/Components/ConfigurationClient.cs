using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class ConfigurationClient : PaginatedClient, IConfigurationClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ConfigurationClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		public ConfigurationClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task DeleteEmptyDirectory(IConfigurationFile directory, CancellationToken cancellationToken) => ApiClient.Delete(Routes.Configuration, directory, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<ConfigurationFileResponse> CreateDirectory(IConfigurationFile directory, CancellationToken cancellationToken) => ApiClient.Create<IConfigurationFile, ConfigurationFileResponse>(Routes.Configuration, directory, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<ConfigurationFileResponse>> List(
			PaginationSettings? paginationSettings,
			string directory,
			CancellationToken cancellationToken)
			=> ReadPaged<ConfigurationFileResponse>(
				paginationSettings,
				Routes.ListRoute(Routes.Configuration) + Routes.SanitizeGetPath(directory),
				instance.Id!.Value,
				cancellationToken);

		/// <inheritdoc />
		public async Task<Tuple<ConfigurationFileResponse, Stream>> Read(IConfigurationFile file, CancellationToken cancellationToken)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			var configFile = await ApiClient.Read<ConfigurationFileResponse>(
				Routes.ConfigurationFile + Routes.SanitizeGetPath(file.Path ?? throw new ArgumentException("file.Path should not be null!", nameof(file))),
				instance.Id!.Value,
				cancellationToken)
				.ConfigureAwait(false);
			var downloadStream = await ApiClient.Download(configFile, cancellationToken).ConfigureAwait(false);
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
		public async Task<ConfigurationFileResponse> Write(ConfigurationFileRequest file, Stream uploadStream, CancellationToken cancellationToken)
		{
			long initialStreamPosition = 0;
			MemoryStream? memoryStream = null;
			if (uploadStream?.CanSeek == false)
				memoryStream = new MemoryStream();
			else if (uploadStream != null)
				initialStreamPosition = uploadStream.Position;

			using (memoryStream)
			{
				var configFileTask = ApiClient.Update<ConfigurationFileRequest, ConfigurationFileResponse>(
					Routes.Configuration,
					file ?? throw new ArgumentNullException(nameof(file)),
					instance.Id!.Value,
					cancellationToken);

				if (memoryStream != null)
					await uploadStream!.CopyToAsync(memoryStream).ConfigureAwait(false);

				var configFile = await configFileTask.ConfigureAwait(false);

				var streamUsed = memoryStream ?? uploadStream;
				streamUsed?.Seek(initialStreamPosition, SeekOrigin.Begin);
				await ApiClient.Upload(configFile, streamUsed, cancellationToken).ConfigureAwait(false);

				return configFile;
			}
		}
	}
}
