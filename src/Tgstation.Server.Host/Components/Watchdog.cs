using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Models;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog, IDisposable
	{
		/// <summary>
		/// The <see cref="IByond"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IByond byond;
		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IChat chat;
		/// <summary>
		/// The <see cref="IDreamDaemonExecutor"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDreamDaemonExecutor dreamDaemonExecutor;
		/// <summary>
		/// The <see cref="IInterop"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IInterop interop;
		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;
		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IInstanceManager instanceManager;
		/// <summary>
		/// The <see cref="Api.Models.Instance.Id"/> of the <see cref="Watchdog"/>
		/// </summary>
		readonly long instanceId;

		/// <summary>
		/// Represents the currently running <see cref="Watchdog"/>
		/// </summary>
		Task watchdogTask;
		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="watchdogTask"/>
		/// </summary>
		CancellationTokenSource watchdogCancellationTokenSource;

		/// <summary>
		/// Construct a <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="dreamDaemonExecutor">The value of <see cref="dreamDaemonExecutor"/></param>
		/// <param name="interop">The value of <see cref="interop"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="instanceId">The value of <see cref="instanceId"/></param>
		public Watchdog(IByond byond, IChat chat, IDreamDaemonExecutor dreamDaemonExecutor, IInterop interop, IDmbFactory dmbFactory, IEventConsumer eventConsumer, IInstanceManager instanceManager, long instanceId)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.dreamDaemonExecutor = dreamDaemonExecutor ?? throw new ArgumentNullException(nameof(dreamDaemonExecutor));
			this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));

			this.instanceId = instanceId;
		}

		/// <inheritdoc />
		public void Dispose() => watchdogCancellationTokenSource?.Dispose();

		/// <summary>
		/// Loads a dmb and runs it through <see cref="dreamDaemonExecutor"/>
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the run</param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed once the server starts if any</param>
		/// <param name="interopInfo">The <see cref="InteropInfo"/> the server</param>
		/// <param name="dreamDaemonPath">The path to the DreamDaemon executable</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the exit code of DreamDaemon</returns>
		async Task<int> RunServer(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, InteropInfo interopInfo, string dreamDaemonPath, CancellationToken cancellationToken)
		{
			using (var dmb = await dmbFactory.LockNextDmb(cancellationToken).ConfigureAwait(false))
			{
				var chatJsonGuid = Guid.NewGuid();
				var isPrimary = interopInfo.NextPort == launchParameters.SecondaryPort;

				//set up chat stuff
				interopInfo.ChatChannelsJson = String.Concat(chatJsonGuid, ".channels.json");
				//only track chat commands from primary server
				if(isPrimary)
					interopInfo.ChatCommandsJson = String.Concat(chatJsonGuid, ".commands.json");

				//set up revision
				interopInfo.Revision = dmb.RevisionInformation;
				foreach (var I in dmb.RevisionInformation.TestMerges)
					interopInfo.TestMerges.Add(new Models.TestMerge
					{
						Author = I.Author,
						Body = I.BodyAtMerge,
						Comment = I.Comment,
						Commit = I.RevisionInformation.Commit,
						OriginRevision = I.RevisionInformation.OriginRevision,
						Number = I.Number,
						PullRequestCommit = I.PullRequestRevision,
						TimeMerged = I.MergedAt.ToUnixTimeSeconds(),
						Title = I.TitleAtMerge,
						Url = I.Url.ToString()
					});

				using (await chat.TrackJsons(isPrimary ? dmb.PrimaryDirectory : dmb.SecondaryDirectory, interopInfo.ChatChannelsJson, isPrimary ? interopInfo.ChatCommandsJson : null, cancellationToken).ConfigureAwait(false))
					return await dreamDaemonExecutor.RunDreamDaemon(launchParameters, onSuccessfulStartup, dreamDaemonPath, dmb, interopInfo, false, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Locks in a <see cref="IByond"/> version and runs a server through <see cref="RunServer(DreamDaemonLaunchParameters, TaskCompletionSource{object}, InteropInfo, string, CancellationToken)"/>
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/> for the run</param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed once the server starts if any</param>
		/// <param name="accessToken">The access token for the server</param>
		/// <param name="isPrimary">If a primary server is being launched</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <param name="cancellationTokenSource">A <see cref="CancellationTokenSource"/> tied to the lifetime of the resulting <see cref="Task{TResult}"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the exit code of DreamDaemon</returns>
		Task<int> StartServer(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, string accessToken, bool isPrimary, CancellationToken cancellationToken, out CancellationTokenSource cancellationTokenSource)
		{
			cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			try
			{
				var ddToken = cancellationTokenSource.Token;

				var interopInfo = new InteropInfo
				{
					AccessToken = accessToken,
					ApiValidateOnly = false,
					HostPath = Application.HostingPath,
					InstanceId = instanceId,
					NextPort = isPrimary ? launchParameters.SecondaryPort : launchParameters.PrimaryPort,
					//this line feels hacky, change it and remove the instanceManager dep?
					InstanceName = instanceManager.GetInstance(new Host.Models.Instance { Id = instanceId }).GetMetadata().Name,
				};
				
				return byond.UseExecutables((dreamMakerPath, dreamDaemonPath) => RunServer(launchParameters, onSuccessfulStartup, interopInfo, dreamDaemonPath, ddToken), false);
			}
			catch
			{
				cancellationTokenSource.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Handle a crash or exit of a server
		/// </summary>
		/// <param name="serverTask">The <see cref="Task{TResult}"/> resulting in the exit code of the server</param>
		/// <param name="isPrimary">If the ended server was the primary server</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the server was ended due to a <see cref="CancellationToken"/>, <see langword="false"/> otherwise</returns>
		async Task<bool> HandleServerCrashed(Task<int> serverTask, bool isPrimary, CancellationToken cancellationToken)
		{
			int exitCode;
			try
			{
				//nothing to do except try and reboot it
				exitCode = await serverTask.ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				return true;
			}
			await eventConsumer.HandleEvent(exitCode == 0 ? (isPrimary ? EventType.DDExit : EventType.DDOtherExit) : (isPrimary ? EventType.DDCrash : EventType.DDOtherCrash), null, cancellationToken).ConfigureAwait(false);
			return false;
		}

		/// <summary>
		/// Handle <see cref="ChatMessageEventArgs"/>
		/// </summary>
		/// <param name="e">The <see cref="ChatMessageEventArgs"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task HandleChatMessage(ChatMessageEventArgs e, CancellationToken cancellationToken)
		{
			if (e == null)
				throw new ArgumentNullException(nameof(e));
			return chat.SendMessage(e.ChatResponse.Message, e.ChatResponse.ChannelIds, cancellationToken);
		}

		/// <summary>
		/// Main <see cref="Watchdog"/> loop
		/// </summary>
		/// <param name="launchParametersFactory">The <see cref="ILaunchParametersFactory"/> for the run</param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> to be completed once the first server starts</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task Run(ILaunchParametersFactory launchParametersFactory, TaskCompletionSource<object> onSuccessfulStartup, CancellationToken cancellationToken)
		{
			if (await byond.GetVersion(cancellationToken).ConfigureAwait(false) == null)
				throw new InvalidOperationException("No byond version installed!");
			await byond.ClearCache(cancellationToken).ConfigureAwait(false);

			var initialLaunchParameters = await launchParametersFactory.GetLaunchParameters(cancellationToken).ConfigureAwait(false);
			var retries = 0;
			do
			{
				var retryDelay = (int)Math.Min(Math.Pow(2, retries), TimeSpan.FromHours(1).Milliseconds); //max of one hour
				await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);

				using (var control = interop.CreateRun(initialLaunchParameters.PrimaryPort, initialLaunchParameters.SecondaryPort, HandleChatMessage))
				{
					var primaryPrimedTcs = new TaskCompletionSource<object>();
					control.OnServerControl += (sender, e) =>
					{
						if (e.FromPrimaryServer && e.EventType == ServerControlEventType.ServerPrimed)
							primaryPrimedTcs.TrySetResult(null);
					};

					//start the primary server
					var ddPrimaryTask = StartServer(initialLaunchParameters, onSuccessfulStartup, control.PrimaryAccessToken, true, cancellationToken, out CancellationTokenSource primaryCts);
					try
					{
						//wait to make sure we got this far
						await onSuccessfulStartup.Task.ConfigureAwait(false);
						onSuccessfulStartup = null;

						//wait for either the server to exit or be primed
						await Task.WhenAny(ddPrimaryTask, primaryPrimedTcs.Task).ConfigureAwait(false);

						if (ddPrimaryTask.IsCompleted)
						{
							if (await HandleServerCrashed(ddPrimaryTask, true, cancellationToken).ConfigureAwait(false))
								return;
							++retries;
							continue;
						}

						var launchParameters = initialLaunchParameters;
						Task<int> ddSecondaryTask = null;
						CancellationTokenSource secondaryCts = null;
						try
						{
							do
							{
								if (ddSecondaryTask == null)
									//start the secondary server
									ddSecondaryTask = StartServer(initialLaunchParameters, null, control.SecondaryAccessToken, false, cancellationToken, out secondaryCts);

								var newDmbTask = dmbFactory.OnNewerDmb();

								//now we wait for something to happen
								await Task.WhenAny(ddSecondaryTask, ddPrimaryTask, newDmbTask).ConfigureAwait(false);

								//some helpers
								void PrimaryRestart()
								{
									primaryCts.Dispose();
									ddPrimaryTask = StartServer(initialLaunchParameters, null, control.PrimaryAccessToken, true, cancellationToken, out primaryCts);
								}
								void SecondaryRestart()
								{
									ddSecondaryTask = null;
									secondaryCts.Dispose();
								};
								Task<bool> PrimaryCrash() => HandleServerCrashed(ddPrimaryTask, control.SecondaryIsOther, cancellationToken);
								Task<bool> SecondaryCrash() => HandleServerCrashed(ddSecondaryTask, !control.SecondaryIsOther, cancellationToken);

								//update available
								if (newDmbTask.IsCompleted)
								{
									//restart the other server but don't treat it as an error
									launchParameters = await launchParametersFactory.GetLaunchParameters(cancellationToken).ConfigureAwait(false);
									//restart other server
									if (control.SecondaryIsOther)
									{
										secondaryCts.Cancel();
										await ddSecondaryTask.ConfigureAwait(false);
										cancellationToken.ThrowIfCancellationRequested();
										SecondaryRestart();
									}
									else
									{
										primaryCts.Cancel();
										await ddPrimaryTask.ConfigureAwait(false);
										cancellationToken.ThrowIfCancellationRequested();
										PrimaryRestart();
									}
									continue;
								}

								//crash of both servers
								if (ddSecondaryTask.IsCompleted && ddPrimaryTask.IsCompleted)
								{
									//catastrophic, start over
									var t1 = PrimaryCrash();
									await Task.WhenAll(t1, SecondaryCrash()).ConfigureAwait(false);
									if (t1.Result)
										return;
									++retries;
									continue;
								}

								//below this point: crash of single server

								//activate the other server and load new launch params
								var otherServerActivation = control.ActivateOtherServer(cancellationToken);
								launchParameters = await launchParametersFactory.GetLaunchParameters(cancellationToken).ConfigureAwait(false);
								await otherServerActivation.ConfigureAwait(false);

								//crash of secondary server
								if (ddSecondaryTask.IsCompleted)
								{
									if (await SecondaryCrash().ConfigureAwait(false))
										return;
									SecondaryRestart();
								}
								//crash of primary server
								else
								{
									if (await PrimaryCrash().ConfigureAwait(false))
										return;
									PrimaryRestart();
								}
							} while (true);
						}
						finally
						{
							secondaryCts?.Dispose();
						}
					}
					finally
					{
						primaryCts.Dispose();
					}
				}
			} while (true);
		}

		/// <inheritdoc />
		public async Task Start(ILaunchParametersFactory launchParametersFactory, CancellationToken cancellationToken)
		{
			TaskCompletionSource<object> taskCompletionSource;
			lock (this)
			{
				if (watchdogTask != null)
					throw new InvalidOperationException("Watchdog already running!");
				watchdogCancellationTokenSource?.Dispose();
				watchdogCancellationTokenSource = new CancellationTokenSource();
				taskCompletionSource = new TaskCompletionSource<object>();
				watchdogTask = Run(launchParametersFactory, taskCompletionSource, watchdogCancellationTokenSource.Token);
			}

			using (cancellationToken.Register(() => watchdogCancellationTokenSource.Cancel()))
				await taskCompletionSource.Task.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task Stop()
		{
			lock(this)
			{
				if (watchdogTask == null)
					throw new InvalidOperationException("Watchdog not running!");
				watchdogCancellationTokenSource.Cancel();
				var task = watchdogTask;
				watchdogTask = null;
				return task;
			}
		}
	}
}
