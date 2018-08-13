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
		public IRepositoryClient Repository => throw new NotImplementedException();

		/// <inheritdoc />
		public IDreamDaemonClient DreamDaemon => throw new NotImplementedException();

		/// <inheritdoc />
		public IConfigurationClient Configuration => throw new NotImplementedException();

		/// <inheritdoc />
		public IInstanceUserClient Users => throw new NotImplementedException();

		/// <inheritdoc />
		public IChatBotsClient ChatBots => throw new NotImplementedException();

		/// <inheritdoc />
		public IDreamMakerClient DreamMaker => throw new NotImplementedException();

		/// <inheritdoc />
		public IJobsClient Jobs => throw new NotImplementedException();

		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="InstanceClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// Construct a <see cref="InstanceClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="Metadata"/></param>
		public InstanceClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			Metadata = instance ?? throw new ArgumentNullException(nameof(instance));

			Byond = new ByondClient(apiClient, instance);
		}
	}
}