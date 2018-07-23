using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class SessionControllerFactory : ISessionControllerFactory
	{
		/// <summary>
		/// The <see cref="IExecutor"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IExecutor executor;

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IByondManager byond;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

		/// <summary>
		/// The <see cref="IInteropRegistrar"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IInteropRegistrar interopRegistrar;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="Models.Instance"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly Models.Instance instance;

		/// <summary>
		/// Construct a <see cref="SessionControllerFactory"/>
		/// </summary>
		/// <param name="executor">The value of <see cref="executor"/></param>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="interopRegistrar">The value of <see cref="interopRegistrar"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public SessionControllerFactory(IExecutor executor, IByondManager byond, IByondTopicSender byondTopicSender, IInteropRegistrar interopRegistrar, ICryptographySuite cryptographySuite, IApplication application, IIOManager ioManager, IChat chat, ILoggerFactory loggerFactory, Models.Instance instance)
		{
			this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.interopRegistrar = interopRegistrar ?? throw new ArgumentNullException(nameof(interopRegistrar));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		/// <inheritdoc />
		public async Task<ISessionController> LaunchNew(DreamDaemonLaunchParameters launchParameters, IDmbProvider dmbProvider, IByondExecutableLock currentByondLock, bool primaryPort, bool primaryDirectory, bool apiValidate, CancellationToken cancellationToken)
		{
			var portToUse = primaryPort ? launchParameters.PrimaryPort : launchParameters.SecondaryPort;
			if (!portToUse.HasValue)
				throw new InvalidOperationException("Given port is null!");
			var accessIdentifier = cryptographySuite.GetSecureString();

			string GuidJsonFile() => String.Format(CultureInfo.InvariantCulture, "{0}.json", Guid.NewGuid());

			var interopInfo = new InteropInfo
			{
				AccessIdentifier = accessIdentifier,
				ApiValidateOnly = apiValidate,
				ChatChannelsJson = GuidJsonFile(),
				ChatCommandsJson = GuidJsonFile(),
				HostPath = application.HostingPath,
				InstanceName = instance.Name,
				Revision = dmbProvider.CompileJob.RevisionInformation
			};
			interopInfo.TestMerges.AddRange(dmbProvider.CompileJob.RevisionInformation.TestMerges.Select(x => new TestMerge
			{
				Author = x.Author,
				Body = x.BodyAtMerge,
				Comment = x.Comment,
				Commit = x.RevisionInformation.Commit,
				Number = x.Number,
				OriginRevision = x.RevisionInformation.OriginRevision,
				PullRequestCommit = x.PullRequestRevision,
				TimeMerged = x.MergedAt.Ticks,
				Title = x.TitleAtMerge,
				Url = x.Url
			}));

			var interopJsonFile = GuidJsonFile();

			var interopJson = JsonConvert.SerializeObject(interopInfo);

			var basePath = primaryDirectory ? dmbProvider.PrimaryDirectory : dmbProvider.SecondaryDirectory;
			var localIoManager = new ResolvingIOManager(ioManager, ioManager.ConcatPath(basePath, dmbProvider.DmbName));

			var chatJsonTrackingTask = chat.TrackJsons(basePath, interopInfo.ChatChannelsJson, interopInfo.ChatCommandsJson, cancellationToken);

			await localIoManager.WriteAllBytes(interopJsonFile, Encoding.UTF8.GetBytes(interopJson), cancellationToken).ConfigureAwait(false);
			var chatJsonTrackingContext = await chatJsonTrackingTask.ConfigureAwait(false);
			try
			{
				var byondLock = currentByondLock ?? byond.UseExecutables(Version.Parse(dmbProvider.CompileJob.ByondVersion));
				try
				{
					//more sanitization here cause it uses the same scheme
					var parameters = String.Format(CultureInfo.InvariantCulture, "{2}={0}&{3}={1}", byondTopicSender.SanitizeString(application.Version.ToString()), byondTopicSender.SanitizeString(interopJsonFile), byondTopicSender.SanitizeString(InteropConstants.DMParamHostVersion), byondTopicSender.SanitizeString(InteropConstants.DMParamInfoJson));

					var session = executor.RunDreamDaemon(launchParameters, byondLock, dmbProvider, parameters, !primaryPort, !primaryDirectory);
					try
					{
						return new SessionController(new ReattachInformation
						{
							AccessIdentifier = accessIdentifier,
							Dmb = dmbProvider,
							IsPrimary = primaryDirectory,
							Port = portToUse.Value,
							ProcessId = session.ProcessId
						}, session, byondTopicSender, interopRegistrar, chatJsonTrackingContext, chat, loggerFactory.CreateLogger<SessionController>());
					}
					catch
					{
						session.Dispose();
						throw;
					}
				}
				catch
				{
					if (currentByondLock == null)
						byondLock.Dispose();
					throw;
				}
			}
			catch
			{
				chatJsonTrackingContext.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async Task<ISessionController> Reattach(ReattachInformation reattachInformation, CancellationToken cancellationToken)
		{
			if (reattachInformation == null)
				throw new ArgumentNullException(nameof(reattachInformation));
			
			var basePath = reattachInformation.IsPrimary ? reattachInformation.Dmb.PrimaryDirectory : reattachInformation.Dmb.SecondaryDirectory;
			var chatJsonTrackingContext = await chat.TrackJsons(basePath, reattachInformation.ChatChannelsJson, reattachInformation.ChatCommandsJson, cancellationToken).ConfigureAwait(false);
			try
			{
				var byondLock = byond.UseExecutables(Version.Parse(reattachInformation.Dmb.CompileJob.ByondVersion));
				try
				{
					var session = executor.AttachToDreamDaemon(reattachInformation.ProcessId, byondLock);
					try
					{
						return new SessionController(reattachInformation, session, byondTopicSender, interopRegistrar, chatJsonTrackingContext, chat, loggerFactory.CreateLogger<SessionController>());
					}
					catch
					{
						session.Dispose();
						throw;
					}
				}
				catch
				{
					byondLock.Dispose();
					throw;
				}
			}
			catch
			{
				chatJsonTrackingContext.Dispose();
				throw;
			}
		}
	}
}
