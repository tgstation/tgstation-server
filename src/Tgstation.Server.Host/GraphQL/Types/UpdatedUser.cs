using System;

using HotChocolate.Types.Relay;

using Tgstation.Server.Host.GraphQL.Transformers;
using Tgstation.Server.Host.Models;

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
		[ID(nameof(Types.User))]
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
			User = ((IApiTransformable<Models.User, User, UserTransformer>)user).ToApi();
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
