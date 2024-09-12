using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Types.Relay;

using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.GraphQL.Interfaces;
using Tgstation.Server.Host.Models.Transformers;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A <see cref="User"/> with limited fields.
	/// </summary>
	[Node]
	public sealed class UserName : NamedEntity, IUserName
	{
		public static async ValueTask<UserName> GetUserName(
			long id,
			[Service] IGraphQLAuthorityInvoker<IUserAuthority> authorityInvoker,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(authorityInvoker);
			var user = await authorityInvoker.InvokeTransformable<Models.User, User, UserGraphQLTransformer>(
				authority => authority.GetId(id, false, true, cancellationToken));

			return new UserName(user);
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
	}
}
