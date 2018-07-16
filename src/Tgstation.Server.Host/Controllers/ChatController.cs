using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

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

		/// <inheritdoc />
		[TgsAuthorize(ChatSettingsRights.Create)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.ChatSettings model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (String.IsNullOrWhiteSpace(model.Name))
				return BadRequest(new { message = "Name cannot be null or whitespace!" });

			if (String.IsNullOrWhiteSpace(model.ConnectionString))
				return BadRequest(new { message = "ConnectionString cannot be null or whitespace!" });

			//try to update das db first
			var dbModel = new Models.ChatSettings
			{
				Name = model.Name,
				ConnectionString = model.ConnectionString,
				Enabled = model.Enabled,
				Channels = model.Channels?.Select(x => new Models.ChatChannel
				{
					DiscordChannelId = x.DiscordChannelId,
					IrcChannel = x.IrcChannel,
					IsAdminChannel = x.IsAdminChannel,
					IsWatchdogChannel = x.IsWatchdogChannel
				}).ToList() ?? new List<Models.ChatChannel>(),
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
					DatabaseContext.ChatChannels.RemoveRange(dbModel.Channels);
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
	}
}
