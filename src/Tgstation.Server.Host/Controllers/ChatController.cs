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
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Z.EntityFramework.Plus;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ModelController{TModel}"/> for managing <see cref="Api.Models.ChatSettings"/>
	/// </summary>
	[TgsAuthorize]
	public sealed class ChatController : ModelController<Api.Models.ChatSettings>
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
		public ChatController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager) : base(databaseContext, authenticationContextFactory)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		static Models.ChatChannel ConvertApiChatChannel(Api.Models.ChatChannel api) => new Models.ChatChannel
		{
			DiscordChannelId = api.DiscordChannelId,
			IrcChannel = api.IrcChannel,
			IsAdminChannel = api.IsAdminChannel,
			IsWatchdogChannel = api.IsWatchdogChannel
		};

		/// <inheritdoc />
		[TgsAuthorize(ChatSettingsRights.Create)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.ChatSettings model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (String.IsNullOrWhiteSpace(model.Name))
				return BadRequest(new { message = "name cannot be null or whitespace!" });

			if (String.IsNullOrWhiteSpace(model.ConnectionString))
				return BadRequest(new { message = "connection_string cannot be null or whitespace!" });

			if (!model.Provider.HasValue)
				return BadRequest(new { message = "provider cannot be null!" });

			if (!model.Enabled.HasValue)
				return BadRequest(new { message = "enabled cannot be null!" });

			//try to update das db first
			var dbModel = new Models.ChatSettings
			{
				Name = model.Name,
				ConnectionString = model.ConnectionString,
				Enabled = model.Enabled,
				Channels = model.Channels?.Select(x => ConvertApiChatChannel(x)).ToList() ?? new List<Models.ChatChannel>(),
				InstanceId = Instance.Id,
				Provider = model.Provider,
			};
			DatabaseContext.ChatSettings.Add(dbModel);
			DatabaseContext.ChatChannels.AddRange(dbModel.Channels);
			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			try
			{
				try
				{
					//try to create it
					var instance = instanceManager.GetInstance(Instance);
					await instance.Chat.ChangeSettings(dbModel, cancellationToken).ConfigureAwait(false);

					if (dbModel.Channels.Count > 0)
						await instance.Chat.ChangeChannels(dbModel.Id, dbModel.Channels, cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					//undo the add
					DatabaseContext.ChatSettings.Remove(dbModel);
					await DatabaseContext.Save(default).ConfigureAwait(false);
					throw;
				}
			}
			catch (InvalidOperationException e)
			{
				return BadRequest(new { message = e.Message });
			}
			return Json(dbModel);
		}

		/// <inheritdoc />
		[TgsAuthorize(ChatSettingsRights.Delete)]
		public override async Task<IActionResult> Delete([FromBody] Api.Models.ChatSettings model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var instance = instanceManager.GetInstance(Instance);
			await Task.WhenAll(instance.Chat.DeleteConnection(model.Id, cancellationToken), DatabaseContext.ChatSettings.Where(x => x.Id == model.Id).DeleteAsync(cancellationToken)).ConfigureAwait(false);

			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(ChatSettingsRights.Read)]
		public override async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var query = DatabaseContext.ChatSettings.Where(x => x.InstanceId == Instance.Id).Include(x => x.Channels);

			var results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

			var connectionStrings = (AuthenticationContext.GetRight(RightsType.ChatSettings) & (int)ChatSettingsRights.ReadConnectionString) != 0;

			if (!connectionStrings)
				foreach (var I in results)
					I.ConnectionString = null;

			return Json(results);
		}

		/// <inheritdoc />
		[TgsAuthorize(ChatSettingsRights.WriteChannels | ChatSettingsRights.WriteConnectionString | ChatSettingsRights.WriteEnabled | ChatSettingsRights.WriteName | ChatSettingsRights.WriteProvider)]
		public override async Task<IActionResult> Update([FromBody] Api.Models.ChatSettings model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var query = DatabaseContext.ChatSettings.Where(x => x.InstanceId == Instance.Id && x.Id == model.Id).Include(x => x.Channels);

			var current = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (current == default)
				return StatusCode((int)HttpStatusCode.Gone);

			var userRights = (ChatSettingsRights)AuthenticationContext.GetRight(RightsType.ChatSettings);

			bool anySettingsModified = false;

			bool CheckModified<T>(Expression<Func<Api.Models.Internal.ChatSettings, T>> expression, ChatSettingsRights requiredRight)
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
			};

			if (!CheckModified(x => x.ConnectionString, ChatSettingsRights.WriteConnectionString)
				|| !CheckModified(x => x.Enabled, ChatSettingsRights.WriteEnabled)
				|| !CheckModified(x => x.Name, ChatSettingsRights.WriteName)
				|| !CheckModified(x => x.Provider, ChatSettingsRights.WriteProvider)
				|| (model.Channels != null && !userRights.HasFlag(ChatSettingsRights.WriteChannels)))
				return Forbid();

			if (model.Channels != null)
			{
				DatabaseContext.ChatChannels.RemoveRange(current.Channels);
				var dbChannels = model.Channels.Select(x => ConvertApiChatChannel(x)).ToList();
				DatabaseContext.ChatChannels.AddRange(dbChannels);
				current.Channels = dbChannels;
			}

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			var chat = instanceManager.GetInstance(Instance).Chat;

			if (anySettingsModified)
				//have to rebuild the thing first
				await chat.ChangeSettings(current, cancellationToken).ConfigureAwait(false);

			if (model.Channels != null)
				await chat.ChangeChannels(current.Id, current.Channels, cancellationToken).ConfigureAwait(false);

			if(userRights.HasFlag(ChatSettingsRights.Read))
				return Json(current);
			return Ok();
		}
	}
}
