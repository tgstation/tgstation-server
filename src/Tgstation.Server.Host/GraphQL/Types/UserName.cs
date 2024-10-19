﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.Models.Transformers;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A <see cref="User"/> with limited fields.
	/// </summary>
	[Node]
	public sealed class UserName : NamedEntity, IUserName
	{
		/// <summary>
		/// Node resolver for <see cref="UserName"/>s.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> to lookup.</param>
		/// <param name="userAuthority">The <see cref="IGraphQLAuthorityInvoker{TAuthority}"/> for the <see cref="IUserAuthority"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the queried <see cref="UserName"/>, if present.</returns>
		[TgsGraphQLAuthorize]
		public static ValueTask<UserName?> GetUserName(
			long id,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> userAuthority,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(userAuthority);
			return userAuthority.InvokeTransformableAllowMissing<Models.User, UserName, UserNameGraphQLTransformer>(
				authority => authority.GetId(id, false, true, cancellationToken));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserName"/> class.
		/// </summary>
		/// <param name="copy">The <see cref="NamedEntity"/> to copy.</param>
		[SetsRequiredMembers]
		public UserName(NamedEntity copy)
			: base(copy)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UserName"/> class.
		/// </summary>
		public UserName()
		{
		}
	}
}
