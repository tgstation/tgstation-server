using System;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.GraphQL.Transformers;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents a <see cref="User"/> that has been updated.
	/// </summary>
	public sealed class UpdatedUser :
		ILegacyApiTransformable<UserResponse>,
		IApiTransformable<UpdatedUser, GraphQL.Types.UpdatedUser, UpdatedUserTransformer>
	{
		/// <summary>
		/// The <see cref="User"/>'s <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long Id { get; }

		/// <summary>
		/// The <see cref="Models.User"/>, if it authorized to be read.
		/// </summary>
		public User? User { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="UpdatedUser"/> class.
		/// </summary>
		/// <param name="user">The value of <see cref="User"/> containing the <see cref="Id"/>.</param>
		public UpdatedUser(User user)
			: this((user ?? throw new ArgumentNullException(nameof(user))).Require(u => u.Id))
		{
			User = user;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UpdatedUser"/> class.
		/// </summary>
		/// <param name="id">The value of <see cref="Id"/>.</param>
		public UpdatedUser(long id)
		{
			Id = id;
		}

		/// <inheritdoc />
		public UserResponse ToApi()
			=> User?.ToApi() ?? new UserResponse
			{
				Id = Id,
			};
	}
}
