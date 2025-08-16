using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc cref="IChatAuthority" />
	sealed class ChatAuthority : ComponentInterfacingAuthorityBase, IChatAuthority
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatAuthority"/> class.
		/// </summary>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="ChatAuthority"/>.</param>
		/// <param name="databaseContext">The <see cref="AuthorityBase.DatabaseContext"/>.</param>
		/// <param name="logger">The <see cref="AuthorityBase.Logger"/>.</param>
		public ChatAuthority(IInstanceManager instanceManager, IDatabaseContext databaseContext, ILogger<ChatAuthority> logger)
			: base(instanceManager, databaseContext, logger)
		{
		}

		/// <summary>
		/// Perform some basic validation of a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="ChatBotApiBase"/> to validate.</param>
		/// <param name="forCreation">If the <paramref name="model"/> is being created.</param>
		/// <returns>An <see cref="BadRequestObjectResult"/> to respond with or <see langword="null"/>.</returns>
		static AuthorityResponse<ChatBot>? StandardModelChecks(ChatBot model, bool forCreation)
		{
			if (model.ReconnectionInterval == 0)
				throw new InvalidOperationException("RecconnectionInterval cannot be zero!");

			if (model.Name != null && String.IsNullOrWhiteSpace(model.Name))
				return BadRequest<ChatBot>(ErrorCode.ChatBotWhitespaceName);

			if (model.ConnectionString != null && String.IsNullOrWhiteSpace(model.ConnectionString))
				return BadRequest<ChatBot>(ErrorCode.ChatBotWhitespaceConnectionString);

			var defaultMaxChannels = (ulong)Math.Max(ChatBot.DefaultChannelLimit, model.Channels?.Count ?? 0);
			if (defaultMaxChannels > UInt16.MaxValue)
				return BadRequest<ChatBot>(ErrorCode.ChatBotMaxChannels);

			if (forCreation)
				model.ChannelLimit ??= (ushort)defaultMaxChannels;

			return null;
		}

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<ChatBot>> Create(
			IEnumerable<Models.ChatChannel> initialChannels,
			string name,
			string connectionString,
			ChatProvider provider,
			long instanceId,
			uint? reconnectionInterval,
			ushort? channelLimit,
			bool enabled,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(initialChannels);
			ArgumentNullException.ThrowIfNull(name);
			ArgumentNullException.ThrowIfNull(connectionString);

			return new(
				() => Flag(ChatBotRights.Create),
				async () =>
				{
					var model = new ChatBot
					{
						Name = name,
						ConnectionString = connectionString,
						Enabled = enabled,
						InstanceId = instanceId,
						Provider = provider,
						ReconnectionInterval = reconnectionInterval,
						ChannelLimit = channelLimit,
						Channels = initialChannels.ToList(),
					};

					var earlyOut = StandardModelChecks(model, true);
					if (earlyOut != null)
						return earlyOut;

					var query = await DatabaseContext
						.Instances
						.AsQueryable()
						.Where(instance => instance.Id == instanceId)
						.Select(instance => new
						{
							ChatBotLimit = instance.ChatBotLimit!.Value,
							TotalChatBots = instance.ChatSettings!.Count,
						})
						.FirstOrDefaultAsync(cancellationToken);

					if (query == null)
						return Gone<ChatBot>();

					if (query.TotalChatBots >= query.ChatBotLimit)
						return Conflict<ChatBot>(ErrorCode.ChatBotMax);

					model.Enabled ??= false;
					model.ReconnectionInterval ??= 1;

					DatabaseContext.ChatBots.Add(model);

					await DatabaseContext.Save(cancellationToken);
					return await WithComponentInstance(
						async instance =>
						{
							try
							{
								// try to create it
								await instance.Chat.ChangeSettings(model, cancellationToken);

								if (model.Channels.Count > 0)
									await instance.Chat.ChangeChannels(model.Require(x => x.Id), model.Channels, cancellationToken);
							}
							catch (Exception ex)
							{
								Logger.LogError(ex, "Failed to complete chat bot {id} initialization after addition, removing...", model.Id);

								// undo the add
								DatabaseContext.ChatBots.Remove(model);

								// DCTx2: Operations must always run
								await DatabaseContext.Save(default);
								await instance.Chat.DeleteConnection(model.Require(x => x.Id), default);
								throw;
							}

							return new AuthorityResponse<ChatBot>(model, HttpSuccessResponse.Created);
						},
						instanceId);
				},
				instanceId);
		}
	}
}
