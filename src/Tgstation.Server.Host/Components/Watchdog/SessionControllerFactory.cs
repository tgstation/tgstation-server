using Byond.TopicSender;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;
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
		/// The <see cref="IByond"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IByond byond;

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
		/// The <see cref="IInstance"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IInstance instance;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IChat chat;

		/// <inheritdoc />
		public async Task<ISessionController> LaunchNew(DreamDaemonLaunchParameters launchParameters, IDmbProvider dmbProvider, bool primaryPort, bool primaryDirectory, bool apiValidate, CancellationToken cancellationToken)
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
				InstanceName = instance.GetMetadata().Name,
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
				var byondLock = byond.UseExecutables(dmbProvider.CompileJob.ByondVersion);
				try
				{
					var parameters = String.Format(CultureInfo.InvariantCulture, "server_service_version={0}&tgs_json={1}", application.Version, interopJsonFile);


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
						}, session, byondTopicSender, interopRegistrar, chatJsonTrackingContext, chat);
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
				chatJsonTrackingTask.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public Task<ISessionController> Reattach(ReattachInformation reattachInformation, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
