using System;
using System.Collections.Generic;
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
		/// Sanitize a <see cref="ConfigurationFile"/> path for use in a GET <see cref="Uri"/>
		/// </summary>
		/// <param name="path">The path to sanitize</param>
		/// <returns>The sanitized path</returns>
		static string SanitizeGetPath(string path)
		{
			if (path == null)
				path = String.Empty;
			if (path.Length == 0 || path[0] != '/')
				path = '/' + path;
			return path;
		}

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
		public Task<IReadOnlyList<ConfigurationFile>> List(string directory, CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<ConfigurationFile>>(Routes.ListRoute(Routes.Configuration) + SanitizeGetPath(directory), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<ConfigurationFile> Read(ConfigurationFile file, CancellationToken cancellationToken)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			return apiClient.Read<ConfigurationFile>(
				Routes.ConfigurationFile + SanitizeGetPath(file.Path ?? throw new ArgumentException("file.Path should not be null!", nameof(file))),
				instance.Id,
				cancellationToken);
		}

		/// <inheritdoc />
		public Task<ConfigurationFile> Write(ConfigurationFile file, CancellationToken cancellationToken) => apiClient.Update<ConfigurationFile, ConfigurationFile>(Routes.Configuration, file ?? throw new ArgumentNullException(nameof(file)), instance.Id, cancellationToken);
	}
}
