using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc cref="Tgstation.Server.Client.Components.IChatBotsClient" />
	sealed class ChatBotsClient : PaginatedClient, IChatBotsClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ChatBotsClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatBotsClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		public ChatBotsClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<ChatBotResponse> Create(ChatBotCreateRequest settings, CancellationToken cancellationToken) => ApiClient.Create<ChatBotCreateRequest, ChatBotResponse>(Routes.Chat, settings ?? throw new ArgumentNullException(nameof(settings)), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task Delete(EntityId settingsId, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.Chat, settingsId?.Id ?? throw new ArgumentNullException(nameof(settingsId))), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<ChatBotResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<ChatBotResponse>(paginationSettings, Routes.ListRoute(Routes.Chat), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<ChatBotResponse> Update(ChatBotUpdateRequest settings, CancellationToken cancellationToken) => ApiClient.Update<ChatBotUpdateRequest, ChatBotResponse>(Routes.Chat, settings ?? throw new ArgumentNullException(nameof(settings)), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<ChatBotResponse> GetId(EntityId settingsId, CancellationToken cancellationToken) => ApiClient.Read<ChatBotResponse>(Routes.SetID(Routes.Chat, settingsId?.Id ?? throw new ArgumentNullException(nameof(settingsId))), instance.Id!.Value, cancellationToken);
	}
}
