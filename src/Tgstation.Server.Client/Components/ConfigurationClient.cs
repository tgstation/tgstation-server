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
		public Task<IReadOnlyList<ConfigurationFile>> List(string directory, CancellationToken cancellationToken)
		{
			if (directory == null)
				directory = String.Empty;
			return apiClient.Read<IReadOnlyList<ConfigurationFile>>(Routes.List(Routes.Configuration) + directory, instance.Id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<ConfigurationFile> Read(ConfigurationFile file, CancellationToken cancellationToken)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			return apiClient.Read<ConfigurationFile>(Routes.Configuration + file.Path, instance.Id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<ConfigurationFile> Write(ConfigurationFile file, CancellationToken cancellationToken) => apiClient.Update<ConfigurationFile, ConfigurationFile>(Routes.Configuration, file, instance.Id, cancellationToken);
	}
}