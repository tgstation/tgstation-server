using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class ChatBotsClient : PaginatedClient, IChatBotsClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ChatBotsClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="ChatBotsClient"/>
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public ChatBotsClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<ChatBot> Create(ChatBot settings, CancellationToken cancellationToken) => ApiClient.Create<ChatBot, ChatBot>(Routes.Chat, settings ?? throw new ArgumentNullException(nameof(settings)), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task Delete(ChatBot settings, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.Chat, settings?.Id ?? throw new ArgumentNullException(nameof(settings))), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<ChatBot>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<ChatBot>(paginationSettings, Routes.ListRoute(Routes.Chat), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<ChatBot> Update(ChatBot settings, CancellationToken cancellationToken) => ApiClient.Update<ChatBot, ChatBot>(Routes.Chat, settings ?? throw new ArgumentNullException(nameof(settings)), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<ChatBot> GetId(ChatBot settings, CancellationToken cancellationToken) => ApiClient.Read<ChatBot>(Routes.SetID(Routes.Chat, (settings ?? throw new ArgumentNullException(nameof(settings))).Id), instance.Id, cancellationToken);
	}
}
