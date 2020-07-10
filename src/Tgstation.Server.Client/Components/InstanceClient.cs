using System;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class InstanceClient : IInstanceClient
	{
		/// <inheritdoc />
		public Instance Metadata { get; }

		/// <inheritdoc />
		public IByondClient Byond { get; }

		/// <inheritdoc />
		public IRepositoryClient Repository { get; }

		/// <inheritdoc />
		public IDreamDaemonClient DreamDaemon { get; }

		/// <inheritdoc />
		public IConfigurationClient Configuration { get; }

		/// <inheritdoc />
		public IInstanceUserClient Users { get; }

		/// <inheritdoc />
		public IChatBotsClient ChatBots { get; }

		/// <inheritdoc />
		public IDreamMakerClient DreamMaker { get; }

		/// <inheritdoc />
		public IJobsClient Jobs { get; }

		/// <summary>
		/// Construct a <see cref="InstanceClient"/>
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> used to construct component clients.</param>
		/// <param name="instance">The value of <see cref="Metadata"/></param>
		public InstanceClient(IApiClient apiClient, Instance instance)
		{
			if (apiClient == null)
				throw new ArgumentNullException(nameof(apiClient));

			Metadata = instance ?? throw new ArgumentNullException(nameof(instance));

			Byond = new ByondClient(apiClient, instance);
			Repository = new RepositoryClient(apiClient, instance);
			DreamDaemon = new DreamDaemonClient(apiClient, instance);
			Configuration = new ConfigurationClient(apiClient, instance);
			Users = new InstanceUserClient(apiClient, instance);
			ChatBots = new ChatBotsClient(apiClient, instance);
			DreamMaker = new DreamMakerClient(apiClient, instance);
			Jobs = new JobsClient(apiClient, instance);
		}
	}
}