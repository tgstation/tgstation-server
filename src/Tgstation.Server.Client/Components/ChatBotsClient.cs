using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class ChatBotsClient : IChatBotsClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="ChatBotsClient"/>
		/// </summary>
		readonly IApiClient apiClient;
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ChatBotsClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="ChatBotsClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public ChatBotsClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<ChatBot> Create(ChatBot settings, CancellationToken cancellationToken) => apiClient.Create<ChatBot, ChatBot>(Routes.Chat, settings, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task Delete(ChatBot settings, CancellationToken cancellationToken) => apiClient.Delete(Routes.SetID(Routes.Chat, settings.Id), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<ChatBot>> List(CancellationToken cancellationToken) => apiClient.Create<IReadOnlyList<ChatBot>>(Routes.List(Routes.Chat), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<ChatBot> Update(ChatBot settings, CancellationToken cancellationToken) => apiClient.Update<ChatBot, ChatBot>(Routes.Chat, settings, instance.Id, cancellationToken);
	}
}