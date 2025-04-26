using System;

using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Models.Transformers;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a <see cref="User"/> that has been updated.
	/// </summary>
	public sealed class UpdatedUser
	{
		/// <summary>
		/// The <see cref="User"/>'s <see cref="Entity.Id"/>.
		/// </summary>
		public long Id { get; }

		/// <summary>
		/// The <see cref="Types.User"/>, if was authorized to be read.
		/// </summary>
		public User? User { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="UpdatedUser"/> class.
		/// </summary>
		/// <param name="user">The value of <see cref="User"/> containing the <see cref="Id"/>.</param>
		public UpdatedUser(Models.User user)
			: this((user ?? throw new ArgumentNullException(nameof(user))).Require(u => u.Id))
		{
			User = ((IApiTransformable<Models.User, User, UserGraphQLTransformer>)user).ToApi();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UpdatedUser"/> class.
		/// </summary>
		/// <param name="id">The value of <see cref="Id"/>.</param>
		public UpdatedUser(long id)
		{
			Id = id;
		}
	}
}
