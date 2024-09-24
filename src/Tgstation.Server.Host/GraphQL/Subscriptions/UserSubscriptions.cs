using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.GraphQL.Types;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.GraphQL.Subscriptions
{
	/// <summary>
	/// Subscriptions for <see cref="User"/>.
	/// </summary>
	[ExtendObjectType(typeof(Subscription))]
	public sealed class UserSubscriptions
	{
		/// <summary>
		/// The name of the topic for when any user is updated.
		/// </summary>
		const string UserUpdatedTopic = "UserUpdated";

		/// <summary>
		/// Get the names of the topics to send to when a <see cref="Models.User"/> is updated.
		/// </summary>
		/// <param name="userId">The <see cref="Api.Models.EntityId.Id"/> of the updated <see cref="Models.User"/>.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of topic <see cref="string"/>s.</returns>
		public static IEnumerable<string> UserUpdatedTopics(long userId)
		{
			yield return UserUpdatedTopic;
			yield return SpecificUserUpdatedTopic(userId);
		}

		/// <summary>
		/// The name of the topic for when a specific <see cref="Models.User"/> is updated.
		/// </summary>
		/// <param name="userId">The <see cref="Api.Models.EntityId.Id"/> of the updated <see cref="Models.User"/>.</param>
		/// <returns>The topic <see cref="string"/>.</returns>
		static string SpecificUserUpdatedTopic(long userId)
			=> $"{UserUpdatedTopic}.{userId}";

		/// <summary>
		/// Receive an update for all <see cref="User"/> changes.
		/// </summary>
		/// <param name="user">The <see cref="Models.User"/> received from the publisher.</param>
		/// <returns>The updated <see cref="User"/>.</returns>
		[Subscribe]
		[TgsGraphQLAuthorize(AdministrationRights.ReadUsers)]
		public User UserUpdated([EventMessage] User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			return user;
		}

		/// <summary>
		/// <see cref="ISourceStream"/> for <see cref="CurrentUserUpdated(User)"/>.
		/// </summary>
		/// <param name="receiver">The <see cref="ITopicEventReceiver"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the request.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="ISourceStream{TMessage}"/> of the <see cref="SessionInvalidationReason"/> for the <paramref name="authenticationContext"/>.</returns>
		public ValueTask<ISourceStream<User>> CurrentUserUpdatedStream(
			[Service] ITopicEventReceiver receiver,
			[Service] IAuthenticationContext authenticationContext,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(receiver);
			ArgumentNullException.ThrowIfNull(authenticationContext);
			return receiver.SubscribeAsync<User>(SpecificUserUpdatedTopic(Models.ModelExtensions.Require(authenticationContext.User, user => user.Id)), cancellationToken);
		}

		/// <summary>
		/// Receive an update to the logged in <see cref="User"/> when it is changed.
		/// </summary>
		/// <param name="user">The <see cref="Models.User"/> received from the publisher.</param>
		/// <returns>The updated <see cref="User"/>.</returns>
		[Subscribe]
		[TgsGraphQLAuthorize]
		public User CurrentUserUpdated([EventMessage] User user)
		{
			ArgumentNullException.ThrowIfNull(user);

			return user;
		}
	}
}
