using System;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Security;

#pragma warning disable CA1724 // conflict with GitLabApiClient.Models.Users. They can fuck off

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Wrapper for accessing <see cref="User"/>s.
	/// </summary>
	public sealed class Users
	{
		/// <summary>
		/// Gets the current <see cref="User"/>.
		/// </summary>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the current <see cref="User"/>.</returns>
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.Read))]
		public ValueTask<User> Current(
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformable<Models.User, User>(authority => authority.Read(cancellationToken));
		}

		/// <summary>
		/// Gets a user by <see cref="Entity.Id"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="User"/>.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="User"/>.</returns>
		[Error(typeof(ErrorMessageException))]
		[TgsGraphQLAuthorize<IUserAuthority>(nameof(IUserAuthority.GetId))]
		public async ValueTask<User?> ById(
			[ID(nameof(User))] long id,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return await userAuthority.InvokeTransformable<Models.User, User>(authority => authority.GetId(id, false, cancellationToken));
		}
	}
}
