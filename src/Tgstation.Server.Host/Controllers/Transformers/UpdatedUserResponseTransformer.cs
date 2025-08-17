using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Controllers.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="UpdatedUser"/>s to <see cref="UserResponse"/>s.
	/// </summary>
	sealed class UpdatedUserResponseTransformer : TransformerBase<UpdatedUser, UserResponse>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UpdatedUserResponseTransformer"/> class.
		/// </summary>
		public UpdatedUserResponseTransformer()
			: base(
				BuildSubProjection<User, UserResponse, UserResponseTransformer>(
					(model, fullUser) => fullUser ?? new UserResponse
					{
						Id = model.Id,
					},
					model => model.User))
		{
		}
	}
}
