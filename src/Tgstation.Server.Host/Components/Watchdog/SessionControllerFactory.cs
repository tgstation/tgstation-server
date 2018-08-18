using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class SessionControllerFactory : ISessionControllerFactory
	{
		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IByondManager"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IByondManager byond;

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;

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
		/// The <see cref="Api.Models.Instance"/> for the <see cref="SessionControllerFactory"/>
		/// </summary>
		readonly Api.Models.Instance instance;

		/// <summary>
		/// Change a given <paramref name="securityLevel"/> into the appropriate DreamDaemon command line word
		/// </summary>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level to change</param>
		/// <returns>A <see cref="string"/> representation of the command line parameter</returns>
		static string SecurityWord(DreamDaemonSecurity securityLevel)
		{
			switch (securityLevel)
			{
				case DreamDaemonSecurity.Safe:
					return "safe";
				case DreamDaemonSecurity.Trusted:
					return "trusted";
				case DreamDaemonSecurity.Ultrasafe:
					return "ultrasafe";
				default:
					throw new ArgumentOutOfRangeException(nameof(securityLevel), securityLevel, String.Format(CultureInfo.InvariantCulture, "Bad DreamDaemon security level: {0}", securityLevel));
			}
		}

		/// <summary>
		/// Construct a <see cref="SessionControllerFactory"/>
		/// </summary>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/></param>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public SessionControllerFactory(IProcessExecutor processExecutor, IByondManager byond, IByondTopicSender byondTopicSender, ICryptographySuite cryptographySuite, IApplication application, IIOManager ioManager, IChat chat, ILoggerFactory loggerFactory, Api.Models.Instance instance)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
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

			const string JsonPostfix = "tgs.json";

			var basePath = primaryDirectory ? dmbProvider.PrimaryDirectory : dmbProvider.SecondaryDirectory;
			//delete all previous tgs json files
			var files = await ioManager.GetFilesWithExtension(basePath, JsonPostfix, cancellationToken).ConfigureAwait(false);

			await Task.WhenAll(files.Select(x => ioManager.DeleteFile(x, cancellationToken))).ConfigureAwait(false);

			//i changed this back from guids, hopefully i don't regret that
			string JsonFile(string name) => String.Format(CultureInfo.InvariantCulture, "{0}.{1}", name, JsonPostfix);

			//setup interop files
			var interopInfo = new JsonFile
			{
				AccessIdentifier = accessIdentifier,
				ApiValidateOnly = apiValidate,
				ChatChannelsJson = JsonFile("chat_channels"),
				ChatCommandsJson = JsonFile("chat_commands"),
				ServerCommandsJson = JsonFile("server_commands"),
				InstanceName = instance.Name,
				Revision = new Api.Models.Internal.RevisionInformation
				{
					CommitSha = dmbProvider.CompileJob.RevisionInformation.CommitSha,
					OriginCommitSha = dmbProvider.CompileJob.RevisionInformation.OriginCommitSha
				}
			};

			interopInfo.TestMerges.AddRange(dmbProvider.CompileJob.RevisionInformation.ActiveTestMerges.Select(x => x.TestMerge).Select(x => new Interop.TestMerge(x, interopInfo.Revision)));

			var interopJsonFile = JsonFile("interop");

			var interopJson = JsonConvert.SerializeObject(interopInfo, Formatting.Indented, new JsonSerializerSettings
			{
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new CamelCaseNamingStrategy()
				},
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});

			var localIoManager = new ResolvingIOManager(ioManager, basePath);

			var chatJsonTrackingTask = chat.TrackJsons(basePath, interopInfo.ChatChannelsJson, interopInfo.ChatCommandsJson, cancellationToken);

			await localIoManager.WriteAllBytes(interopJsonFile, Encoding.UTF8.GetBytes(interopJson), cancellationToken).ConfigureAwait(false);
			var chatJsonTrackingContext = await chatJsonTrackingTask.ConfigureAwait(false);
			try
			{
				//get the byond lock
				var byondLock = currentByondLock ?? await byond.UseExecutables(Version.Parse(dmbProvider.CompileJob.ByondVersion), cancellationToken).ConfigureAwait(false);
				try
				{
					//create interop context
					var context = new CommContext(ioManager, loggerFactory.CreateLogger<CommContext>(), basePath, interopInfo.ServerCommandsJson);
					try
					{
						//set command line options
						//more sanitization here cause it uses the same scheme
						var parameters = String.Format(CultureInfo.InvariantCulture, "{2}={0}&{3}={1}", byondTopicSender.SanitizeString(application.Version.ToString()), byondTopicSender.SanitizeString(interopJsonFile), byondTopicSender.SanitizeString(Constants.DMParamHostVersion), byondTopicSender.SanitizeString(Constants.DMParamInfoJson));

						var arguments = String.Format(CultureInfo.InvariantCulture, "{0} -port {1} {2}-close -{3} -verbose -public -params \"{4}\"",
							dmbProvider.DmbName,
							primaryPort ? launchParameters.PrimaryPort : launchParameters.SecondaryPort,
							launchParameters.AllowWebClient.Value ? "-webclient " : String.Empty,
							SecurityWord(launchParameters.SecurityLevel.Value),
							parameters);

						//launch dd
						var process = processExecutor.LaunchProcess(byondLock.DreamDaemonPath, basePath, arguments);
						try
						{
							//return the session controller for it
							return new SessionController(new ReattachInformation
							{
								AccessIdentifier = accessIdentifier,
								Dmb = dmbProvider,
								IsPrimary = primaryDirectory,
								Port = portToUse.Value,
								ProcessId = process.Id,
								ChatChannelsJson = interopInfo.ChatChannelsJson,
								ChatCommandsJson = interopInfo.ChatCommandsJson,
								ServerCommandsJson = interopInfo.ServerCommandsJson,
							}, process, byondLock, byondTopicSender, chatJsonTrackingContext, context, chat, loggerFactory.CreateLogger<SessionController>(), launchParameters.StartupTimeout);
						}
						catch
						{
							process.Dispose();
							throw;
						}
					}
					catch
					{
						context.Dispose();
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
				var byondLock = await byond.UseExecutables(Version.Parse(reattachInformation.Dmb.CompileJob.ByondVersion), cancellationToken).ConfigureAwait(false);
				try
				{
					var context = new CommContext(ioManager, loggerFactory.CreateLogger<CommContext>(), basePath, reattachInformation.ServerCommandsJson);
					try
					{
						var process = processExecutor.GetProcess(reattachInformation.ProcessId);
						try
						{
							return new SessionController(reattachInformation, process, byondLock, byondTopicSender, chatJsonTrackingContext, context, chat, loggerFactory.CreateLogger<SessionController>(), null);
						}
						catch
						{
							process.Dispose();
							throw;
						}
					}
					catch
					{
						context.Dispose();
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
