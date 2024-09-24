using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate.Subscriptions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.GraphQL;
using Tgstation.Server.Host.GraphQL.Types;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class SessionInvalidationTracker : ISessionInvalidationTracker
	{
		/// <summary>
		/// The <see cref="ITopicEventSender"/> for the <see cref="SessionInvalidationTracker"/>.
		/// </summary>
		readonly ITopicEventSender eventSender;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="SessionInvalidationTracker"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IHostApplicationLifetime"/> for the <see cref="SessionInvalidationTracker"/>.
		/// </summary>
		readonly IHostApplicationLifetime applicationLifetime;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SessionInvalidationTracker"/>.
		/// </summary>
		readonly ILogger<SessionInvalidationTracker> logger;

		/// <summary>
		/// <see cref="ConcurrentDictionary{TKey, TValue}"/> of tracked <see cref="IAuthenticationContext.SessionId"/>s and <see cref="Models.User"/> <see cref="Api.Models.EntityId.Id"/>s to the <see cref="TaskCompletionSource{TResult}"/> for their <see cref="SessionInvalidationReason"/>s.
		/// </summary>
		readonly ConcurrentDictionary<(string SessionId, long UserId), TaskCompletionSource<SessionInvalidationReason>> trackedSessions;

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionInvalidationTracker"/> class.
		/// </summary>
		/// <param name="eventSender">The value of <see cref="eventSender"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="applicationLifetime">The value of <see cref="applicationLifetime"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public SessionInvalidationTracker(
			ITopicEventSender eventSender,
			IAsyncDelayer asyncDelayer,
			IHostApplicationLifetime applicationLifetime,
			ILogger<SessionInvalidationTracker> logger)
		{
			this.eventSender = eventSender ?? throw new ArgumentNullException(nameof(eventSender));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			trackedSessions = new ConcurrentDictionary<(string, long), TaskCompletionSource<SessionInvalidationReason>>();
		}

		/// <inheritdoc />
		public void TrackSession(IAuthenticationContext authenticationContext)
		{
			trackedSessions.GetOrAdd(
				(authenticationContext.SessionId, authenticationContext.User.Require(x => x.Id)),
				tuple =>
				{
					var (localSessionId, localUserId) = tuple;
					logger.LogTrace("Tracking session ID for user {userId}: {sessionId}", localUserId, localSessionId);
					var tcs = new TaskCompletionSource<SessionInvalidationReason>();
					async void SendInvalidationTopic()
					{
						try
						{
							SessionInvalidationReason invalidationReason;
							try
							{
								var otherCancellationReason = tcs.Task;
								var timeTillSessionExpiry = authenticationContext.SessionExpiry - DateTimeOffset.UtcNow;
								if (timeTillSessionExpiry > TimeSpan.Zero)
								{
									var delayTask = asyncDelayer.Delay(timeTillSessionExpiry, applicationLifetime.ApplicationStopping);

									await Task.WhenAny(delayTask, otherCancellationReason);

									if (delayTask.IsCompleted)
										await delayTask;
								}

								invalidationReason = otherCancellationReason.IsCompleted
									? await otherCancellationReason
									: SessionInvalidationReason.TokenExpired;

								logger.LogTrace("Invalidating session ID {sessionID}: {reason}", localSessionId, invalidationReason);
							}
							catch (OperationCanceledException ex)
							{
								logger.LogTrace(ex, "Invalidating session ID {sessionID} due to server shutdown", localSessionId);
								invalidationReason = SessionInvalidationReason.ServerShutdown;
							}

							var topicName = Subscription.SessionInvalidatedTopic(authenticationContext);
							await eventSender.SendAsync(topicName, invalidationReason, CancellationToken.None); // DCT: Session close messages should always be sent
							await eventSender.CompleteAsync(topicName);
						}
						catch (Exception ex)
						{
							logger.LogError(ex, "Error tracking session {sessionId}!", localSessionId);
						}
					}

					SendInvalidationTopic();
					return tcs;
				});
		}

		/// <inheritdoc />
		public void UserModifiedInvalidateSessions(Models.User user)
		{
			ArgumentNullException.ThrowIfNull(user);

			var userId = user.Require(x => x.Id);
			user.LastPasswordUpdate = DateTimeOffset.UtcNow;

			foreach (var key in trackedSessions
				.Keys
				.Where(key => key.UserId == userId)
				.ToList())
				if (trackedSessions.TryRemove(key, out var tcs))
					tcs.TrySetResult(SessionInvalidationReason.UserUpdated);
		}
	}
}
