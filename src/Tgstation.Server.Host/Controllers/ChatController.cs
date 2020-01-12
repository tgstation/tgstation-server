using System;
using System.Collections.Generic;
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
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Z.EntityFramework.Plus;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing <see cref="Api.Models.ChatBot"/>s
	/// </summary>
	[Route(Routes.Chat)]
	public sealed class ChatController : ApiController
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="ChatController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// Construct a <see cref="ChatController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public ChatController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, ILogger<ChatController> logger) : base(databaseContext, authenticationContextFactory, logger, true, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <summary>
		/// Converts <paramref name="api"/> to a <see cref="ChatChannel"/>
		/// </summary>
		/// <param name="api">The <see cref="Api.Models.ChatChannel"/> </param>
		/// <returns>A <see cref="ChatChannel"/> based on <paramref name="api"/></returns>
		static Models.ChatChannel ConvertApiChatChannel(Api.Models.ChatChannel api) => new Models.ChatChannel
		{
			DiscordChannelId = api.DiscordChannelId,
			IrcChannel = api.IrcChannel,
			IsAdminChannel = api.IsAdminChannel ?? false,
			IsWatchdogChannel = api.IsWatchdogChannel ?? false,
			IsUpdatesChannel = api.IsUpdatesChannel ?? false,
			Tag = api.Tag
		};

		/// <summary>
		/// Create a new chat bot <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.ChatBot"/> to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="201">Created chat bot successfully.</response>
		[HttpPut]
		[TgsAuthorize(ChatBotRights.Create)]
		[ProducesResponseType(typeof(Api.Models.ChatBot), 201)]
		public async Task<IActionResult> Create([FromBody] Api.Models.ChatBot model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (String.IsNullOrWhiteSpace(model.Name))
				return BadRequest(new ErrorMessage { Message = "name cannot be null or whitespace!" });

			if (String.IsNullOrWhiteSpace(model.ConnectionString))
				return BadRequest(new ErrorMessage { Message = "connection_string cannot be null or whitespace!" });

			if (!model.Provider.HasValue)
				return BadRequest(new ErrorMessage { Message = "provider cannot be null!" });

			switch (model.Provider)
			{
				case ChatProvider.Discord:
				case ChatProvider.Irc:
					break;
				default:
					return BadRequest(new ErrorMessage { Message = "Invalid provider!" });
			}

			if (!model.ValidateProviderChannelTypes())
				return BadRequest(new ErrorMessage { Message = "One or more of channels aren't formatted correctly for the given provider!" });

			model.Enabled = model.Enabled ?? false;

			// try to update das db first
			var dbModel = new Models.ChatBot
			{
				Name = model.Name,
				ConnectionString = model.ConnectionString,
				Enabled = model.Enabled,
				Channels = model.Channels?.Select(x => ConvertApiChatChannel(x)).ToList() ?? new List<Models.ChatChannel>(), // important that this isn't null
				InstanceId = Instance.Id,
				Provider = model.Provider,
			};

			DatabaseContext.ChatBots.Add(dbModel);

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			try
			{
				try
				{
					// try to create it
					var instance = instanceManager.GetInstance(Instance);
					await instance.Chat.ChangeSettings(dbModel, cancellationToken).ConfigureAwait(false);

					if (dbModel.Channels.Count > 0)
						await instance.Chat.ChangeChannels(dbModel.Id, dbModel.Channels, cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					// undo the add
					DatabaseContext.ChatBots.Remove(dbModel);
					await DatabaseContext.Save(default).ConfigureAwait(false);
					throw;
				}
			}
			catch (InvalidOperationException e)
			{
				return BadRequest(new ErrorMessage { Message = e.Message });
			}

			return StatusCode((int)HttpStatusCode.Created, dbModel.ToApi());
		}

		/// <summary>
		/// Delete a <see cref="Api.Models.ChatBot"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.Internal.ChatBot.Id"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Chat bot deleted or does not exist.</response>
		[HttpDelete("{id}")]
		[TgsAuthorize(ChatBotRights.Delete)]
		[ProducesResponseType(200)]
		public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);
			await Task.WhenAll(instance.Chat.DeleteConnection(id, cancellationToken), DatabaseContext.ChatBots.Where(x => x.Id == id).DeleteAsync(cancellationToken)).ConfigureAwait(false);

			return Ok();
		}

		/// <summary>
		/// List <see cref="Api.Models.ChatBot"/>s.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Listed chat bots successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ChatBotRights.Read)]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.ChatBot>), 200)]
		public async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var query = DatabaseContext.ChatBots.Where(x => x.InstanceId == Instance.Id).Include(x => x.Channels);

			var results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

			var connectionStrings = (AuthenticationContext.GetRight(RightsType.ChatBots) & (ulong)ChatBotRights.ReadConnectionString) != 0;

			if (!connectionStrings)
				foreach (var I in results)
					I.ConnectionString = null;

			return Json(results.Select(x => x.ToApi()));
		}

		/// <summary>
		/// Get a specific <see cref="Api.Models.ChatBot"/>.
		/// </summary>
		/// <param name="id">The <see cref="Api.Models.Internal.ChatBot.Id"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.ChatBot"/> successfully.</response>
		/// <response code="410">Chat bot does not exist.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(ChatBotRights.Read)]
		[ProducesResponseType(typeof(Api.Models.ChatBot), 200)]
		[ProducesResponseType(410)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var query = DatabaseContext.ChatBots.Where(x => x.Id == id).Include(x => x.Channels);

			var results = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (results == default)
				return StatusCode((int)HttpStatusCode.Gone);

			var connectionStrings = (AuthenticationContext.GetRight(RightsType.ChatBots) & (ulong)ChatBotRights.ReadConnectionString) != 0;

			if (!connectionStrings)
				results.ConnectionString = null;

			return Json(results.ToApi());
		}

		/// <summary>
		/// Updates a chat bot <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.ChatBot"/> update to apply.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Update applied successfully. <see cref="Api.Models.ChatBot"/> may or may not be returned based on user permissions.</response>
		[HttpPost]
		[TgsAuthorize(ChatBotRights.WriteChannels | ChatBotRights.WriteConnectionString | ChatBotRights.WriteEnabled | ChatBotRights.WriteName | ChatBotRights.WriteProvider)]
		[ProducesResponseType(200)]
		[ProducesResponseType(typeof(Api.Models.ChatBot), 200)]
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IActionResult> Update([FromBody] Api.Models.ChatBot model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Provider.HasValue && !model.ValidateProviderChannelTypes())
				return BadRequest(new ErrorMessage { Message = "One or more of channels aren't formatted correctly for the given provider!" });

			var query = DatabaseContext.ChatBots.Where(x => x.InstanceId == Instance.Id && x.Id == model.Id).Include(x => x.Channels);

			var current = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (current == default)
				return StatusCode((int)HttpStatusCode.Gone);

			var userRights = (ChatBotRights)AuthenticationContext.GetRight(RightsType.ChatBots);

			bool anySettingsModified = false;

			bool CheckModified<T>(Expression<Func<Api.Models.Internal.ChatBot, T>> expression, ChatBotRights requiredRight)
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
				|| (model.Channels != null && !userRights.HasFlag(ChatBotRights.WriteChannels)))
				return Forbid();

			var hasChannels = model.Channels != null;
			if (hasChannels || (model.Provider.HasValue && model.Provider != oldProvider))
			{
				DatabaseContext.ChatChannels.RemoveRange(current.Channels);
				if (hasChannels)
				{
					var dbChannels = model.Channels.Select(x => ConvertApiChatChannel(x)).ToList();
					DatabaseContext.ChatChannels.AddRange(dbChannels);
					current.Channels = dbChannels;
				}
				else
					current.Channels.Clear();
			}

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			var chat = instanceManager.GetInstance(Instance).Chat;

			if (anySettingsModified)
				await chat.ChangeSettings(current, cancellationToken).ConfigureAwait(false); // have to rebuild the thing first

			if (model.Channels != null || anySettingsModified)
				await chat.ChangeChannels(current.Id, current.Channels, cancellationToken).ConfigureAwait(false);

			if (userRights.HasFlag(ChatBotRights.Read))
			{
				if (!userRights.HasFlag(ChatBotRights.ReadConnectionString))
					current.ConnectionString = null;
				return Json(current.ToApi());
			}

			return Ok();
		}
		#pragma warning restore CA1506
	}
}
