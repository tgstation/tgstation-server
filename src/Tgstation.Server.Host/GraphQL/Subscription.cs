using System;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

using Tgstation.Server.Host.GraphQL.Subscriptions;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// Root type for GraphQL subscriptions.
	/// </summary>
	/// <remarks>Intentionally left mostly empty, use type extensions to properly scope operations to domains.</remarks>
	public sealed class Subscription
	{
		/// <summary>
		/// Gets the topic name for the login session represented by a given <paramref name="authenticationContext"/>.
		/// </summary>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to generate the topic for.</param>
		/// <returns>The <see cref="SessionInvalidated(SessionInvalidationReason)"/> topic for the given <paramref name="authenticationContext"/>.</returns>
		public static string SessionInvalidatedTopic(IAuthenticationContext authenticationContext)
		{
			ArgumentNullException.ThrowIfNull(authenticationContext);
			return $"SessionInvalidated.{authenticationContext.SessionId}";
		}

		/// <summary>
		/// <see cref="ISourceStream"/> for <see cref="SessionInvalidated(SessionInvalidationReason)"/>.
		/// </summary>
		/// <param name="receiver">The <see cref="ITopicEventReceiver"/>.</param>
		/// <param name="invalidationTracker">The <see cref="ISessionInvalidationTracker"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the request.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="ISourceStream{TMessage}"/> of the <see cref="SessionInvalidationReason"/> for the <paramref name="authenticationContext"/>.</returns>
		public ValueTask<ISourceStream<SessionInvalidationReason>> SessionInvalidatedStream(
			[Service] ITopicEventReceiver receiver,
			[Service] ISessionInvalidationTracker invalidationTracker,
			[Service] IAuthenticationContext authenticationContext)
		{
			ArgumentNullException.ThrowIfNull(receiver);
			ArgumentNullException.ThrowIfNull(invalidationTracker);

			var subscription = receiver.SubscribeAsync<SessionInvalidationReason>(SessionInvalidatedTopic(authenticationContext));
			invalidationTracker.TrackSession(authenticationContext);
			return subscription;
		}

		/// <summary>
		/// Receive a <see cref="SessionInvalidationReason"/> immediately before the current login session is invalidated.
		/// </summary>
		/// <param name="sessionInvalidationReason">The <see cref="SessionInvalidationReason"/> received from the publisher.</param>
		/// <returns>The <see cref="SessionInvalidationReason"/>.</returns>
		[Subscribe(With = nameof(SessionInvalidatedStream))]
		[TgsGraphQLAuthorize]
		public SessionInvalidationReason SessionInvalidated([EventMessage] SessionInvalidationReason sessionInvalidationReason)
			=> sessionInvalidationReason;
	}
}
