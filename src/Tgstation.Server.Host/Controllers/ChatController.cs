using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="ChatBot"/>s.
	/// </summary>
	[Route(Routes.Chat)]
#pragma warning disable CA1506 // TODO: Decomplexify
	public sealed class ChatController : InstanceRequiredController
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChatController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="InstanceRequiredController"/>.</param>
		public ChatController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<ChatController> logger,
			IInstanceManager instanceManager,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders)
		{
		}

		/// <summary>
		/// Converts <paramref name="api"/> to a <see cref="ChatChannel"/>.
		/// </summary>
		/// <param name="api">The <see cref="Api.Models.ChatChannel"/>. </param>
		/// <param name="chatProvider">The channel's <see cref="ChatProvider"/>.</param>
		/// <returns>A <see cref="ChatChannel"/> based on <paramref name="api"/>.</returns>
		static Models.ChatChannel ConvertApiChatChannel(Api.Models.ChatChannel api, ChatProvider chatProvider)
		{
			var result = new Models.ChatChannel
			{
				IsAdminChannel = api.IsAdminChannel ?? false,
				IsWatchdogChannel = api.IsWatchdogChannel ?? false,
				IsUpdatesChannel = api.IsUpdatesChannel ?? false,
				IsSystemChannel = api.IsSystemChannel ?? false,
				Tag = api.Tag,
			};

			if (api.ChannelData != null)
			{
				switch (chatProvider)
				{
					case ChatProvider.Discord:
						result.DiscordChannelId = ulong.Parse(api.ChannelData, CultureInfo.InvariantCulture);
						break;
					case ChatProvider.Irc:
						result.IrcChannel = api.ChannelData;
						break;
					default:
						throw new InvalidOperationException($"Invalid chat provider: {chatProvider}");
				}
			}

			return result;
		}

		/// <summary>
		/// Create a new chat bot <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="ChatBotCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="201">Created <see cref="ChatBot"/> successfully.</response>
		[HttpPut]
		[HttpPost(Routes.Create)]
		[TgsAuthorize(ChatBotRights.Create)]
		[ProducesResponseType(typeof(ChatBotResponse), 201)]
		public async ValueTask<IActionResult> Create([FromBody] ChatBotCreateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			var earlyOut = StandardModelChecks(model, true);
			if (earlyOut != null)
				return earlyOut;

			var countOfExistingBotsInInstance = await DatabaseContext
				.ChatBots
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.CountAsync(cancellationToken);

			if (countOfExistingBotsInInstance >= Instance.ChatBotLimit!.Value)
				return Conflict(new ErrorMessageResponse(ErrorCode.ChatBotMax));

			model.Enabled ??= false;
			model.ReconnectionInterval ??= 1;

			// try to update das db first
			var newChannels = model.Channels?.Select(x => ConvertApiChatChannel(x, model.Provider!.Value)).ToList() ?? new List<Models.ChatChannel>(); // important that this isn't null
			var dbModel = new ChatBot(newChannels)
			{
				Name = model.Name,
				ConnectionString = model.ConnectionString,
				Enabled = model.Enabled,
				InstanceId = Instance.Id!.Value,
				Provider = model.Provider,
				ReconnectionInterval = model.ReconnectionInterval,
				ChannelLimit = model.ChannelLimit,
			};

			DatabaseContext.ChatBots.Add(dbModel);

			await DatabaseContext.Save(cancellationToken);
			return await WithComponentInstanceNullable(
				async instance =>
				{
					try
					{
						// try to create it
						await instance.Chat.ChangeSettings(dbModel, cancellationToken);

						if (dbModel.Channels.Count > 0)
							await instance.Chat.ChangeChannels(dbModel.Id!.Value, dbModel.Channels, cancellationToken);
					}
					catch
					{
						// undo the add
						DatabaseContext.ChatBots.Remove(dbModel);

						// DCTx2: Operations must always run
						await DatabaseContext.Save(default);
						await instance.Chat.DeleteConnection(dbModel.Id!.Value, default);
						throw;
					}

					return null;
				})

				?? this.StatusCode(HttpStatusCode.Created, dbModel.ToApi());
		}

		/// <summary>
		/// Delete a <see cref="ChatBot"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="204">Chat bot deleted or does not exist.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(ChatBotRights.Delete)]
		[ProducesResponseType(204)]
		public async ValueTask<IActionResult> Delete(long id, CancellationToken cancellationToken)
			=> await WithComponentInstanceNullable(
				async instance =>
				{
					await Task.WhenAll(
						instance.Chat.DeleteConnection(id, cancellationToken),
						DatabaseContext
							.ChatBots
							.AsQueryable()
							.Where(x => x.Id == id)
							.ExecuteDeleteAsync(cancellationToken));
					return null;
				})

				?? NoContent();

		/// <summary>
		/// List <see cref="ChatBot"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Listed chat bots successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ChatBotRights.Read)]
		[ProducesResponseType(typeof(PaginatedResponse<ChatBotResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
		{
			var connectionStrings = (AuthenticationContext.GetRight(RightsType.ChatBots) & (ulong)ChatBotRights.ReadConnectionString) != 0;
			return Paginated<ChatBot, ChatBotResponse>(
				() => ValueTask.FromResult<PaginatableResult<ChatBot>?>(
					new PaginatableResult<ChatBot>(
						DatabaseContext
							.ChatBots
							.AsQueryable()
							.Where(x => x.InstanceId == Instance.Id)
							.Include(x => x.Channels)
							.OrderBy(x => x.Id))),
				chatBot =>
				{
					if (!connectionStrings)
						chatBot.ConnectionString = null;

					return ValueTask.CompletedTask;
				},
				page,
				pageSize,
				cancellationToken);
		}

		/// <summary>
		/// Get a specific <see cref="ChatBot"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved <see cref="ChatBot"/> successfully.</response>
		/// <response code="410">The <see cref="ChatBot"/> with the given ID does not exist in this instance.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(ChatBotRights.Read)]
		[ProducesResponseType(typeof(ChatBotResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var query = DatabaseContext.ChatBots
				.AsQueryable()
				.Where(x => x.Id == id && x.InstanceId == Instance.Id)
				.Include(x => x.Channels);

			var results = await query.FirstOrDefaultAsync(cancellationToken);
			if (results == default)
				return this.Gone();

			var connectionStrings = (AuthenticationContext.GetRight(RightsType.ChatBots) & (ulong)ChatBotRights.ReadConnectionString) != 0;

			if (!connectionStrings)
				results.ConnectionString = null;

			return Json(results.ToApi());
		}

		/// <summary>
		/// Updates a chat bot <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="ChatBotUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Update applied successfully.</response>
		/// <response code="204">Update applied successfully. <see cref="ChatBot"/> not returned based on user permissions.</response>
		/// <response code="410">The <see cref="ChatBot"/> with the given ID does not exist in this instance.</response>
		[HttpPost]
		[TgsAuthorize(ChatBotRights.WriteChannels | ChatBotRights.WriteConnectionString | ChatBotRights.WriteEnabled | ChatBotRights.WriteName | ChatBotRights.WriteProvider)]
		[ProducesResponseType(typeof(ChatBotResponse), 200)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1502, CA1506 // TODO: Decomplexify
		public async ValueTask<IActionResult> Update([FromBody] ChatBotUpdateRequest model, CancellationToken cancellationToken)
#pragma warning restore CA1502, CA1506
		{
			ArgumentNullException.ThrowIfNull(model);

			IActionResult? earlyOut = StandardModelChecks(model, false);
			if (earlyOut != null)
				return earlyOut;

			var query = DatabaseContext
				.ChatBots
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id && x.Id == model.Id)
				.Include(x => x.Channels);

			var current = await query.FirstOrDefaultAsync(cancellationToken);

			if (current == default)
				return this.Gone();

			if ((model.Channels?.Count ?? current.Channels!.Count) > (model.ChannelLimit ?? current.ChannelLimit!.Value))
			{
				// 400 or 409 depends on if the client sent both
				var errorMessage = new ErrorMessageResponse(ErrorCode.ChatBotMaxChannels);
				if (model.Channels != null && model.ChannelLimit.HasValue)
					return BadRequest(errorMessage);
				return Conflict(errorMessage);
			}

			var userRights = (ChatBotRights)AuthenticationContext.GetRight(RightsType.ChatBots);

			bool anySettingsModified = false;

			bool CheckModified<T>(Expression<Func<ChatBotSettings, T>> expression, ChatBotRights requiredRight)
			{
				var memberSelectorExpression = (MemberExpression)expression.Body;
				var property = (PropertyInfo)memberSelectorExpression.Member;

				var newVal = property.GetValue(model);
				if (newVal == null)
					return false;
				if (!userRights.HasFlag(requiredRight) && property.GetValue(current) != newVal)
					return true;

				property.SetValue(current, newVal);
				anySettingsModified = true;
				return false;
			}

			var oldProvider = current.Provider;

			if (CheckModified(x => x.ConnectionString, ChatBotRights.WriteConnectionString)
				|| CheckModified(x => x.Enabled, ChatBotRights.WriteEnabled)
				|| CheckModified(x => x.Name, ChatBotRights.WriteName)
				|| CheckModified(x => x.Provider, ChatBotRights.WriteProvider)
				|| CheckModified(x => x.ReconnectionInterval, ChatBotRights.WriteReconnectionInterval)
				|| CheckModified(x => x.ChannelLimit, ChatBotRights.WriteChannelLimit)
				|| (model.Channels != null && !userRights.HasFlag(ChatBotRights.WriteChannels)))
				return Forbid();

			var hasChannels = model.Channels != null;
			if (hasChannels || (model.Provider.HasValue && model.Provider != oldProvider))
			{
				DatabaseContext.ChatChannels.RemoveRange(current.Channels!);
				if (hasChannels)
				{
					var dbChannels = model.Channels!.Select(x => ConvertApiChatChannel(x, model.Provider ?? current.Provider!.Value)).ToList();
					DatabaseContext.ChatChannels.AddRange(dbChannels);
					current.Channels = dbChannels;
				}
				else
					current.Channels!.Clear();
			}

			await DatabaseContext.Save(cancellationToken);

			earlyOut = await WithComponentInstanceNullable(
				async instance =>
				{
					var chat = instance.Chat;
					if (anySettingsModified)
						await chat.ChangeSettings(current, cancellationToken); // have to rebuild the thing first

					if ((model.Channels != null || anySettingsModified) && current.Enabled!.Value)
						await chat.ChangeChannels(current.Id!.Value, current.Channels, cancellationToken);

					return null;
				});
			if (earlyOut != null)
				return earlyOut;

			if (userRights.HasFlag(ChatBotRights.Read))
			{
				if (!userRights.HasFlag(ChatBotRights.ReadConnectionString))
					current.ConnectionString = null;
				return Json(current.ToApi());
			}

			return NoContent();
		}

		/// <summary>
		/// Perform some basic validation of a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="ChatBotApiBase"/> to validate.</param>
		/// <param name="forCreation">If the <paramref name="model"/> is being created.</param>
		/// <returns>An <see cref="BadRequestObjectResult"/> to respond with or <see langword="null"/>.</returns>
		BadRequestObjectResult? StandardModelChecks(ChatBotApiBase model, bool forCreation)
		{
			if (model.ReconnectionInterval == 0)
				throw new InvalidOperationException("RecconnectionInterval cannot be zero!");

			if (forCreation && !model.Provider.HasValue)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ChatBotProviderMissing));

			if (model.Name != null && String.IsNullOrWhiteSpace(model.Name))
				return BadRequest(new ErrorMessageResponse(ErrorCode.ChatBotWhitespaceName));

			if (model.ConnectionString != null && String.IsNullOrWhiteSpace(model.ConnectionString))
				return BadRequest(new ErrorMessageResponse(ErrorCode.ChatBotWhitespaceConnectionString));

			if (!model.ValidateProviderChannelTypes())
				return BadRequest(new ErrorMessageResponse(ErrorCode.ChatBotWrongChannelType));

			var defaultMaxChannels = (ulong)Math.Max(ChatBot.DefaultChannelLimit, model.Channels?.Count ?? 0);
			if (defaultMaxChannels > UInt16.MaxValue)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ChatBotMaxChannels));

			if (forCreation)
				model.ChannelLimit ??= (ushort)defaultMaxChannels;

			return null;
		}
	}
#pragma warning restore CA1506
}
