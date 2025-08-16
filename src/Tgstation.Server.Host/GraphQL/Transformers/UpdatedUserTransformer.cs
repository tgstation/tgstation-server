using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="UpdatedUser"/>s.
	/// </summary>
	sealed class UpdatedUserTransformer : Models.TransformerBase<Models.UpdatedUser, UpdatedUser>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UpdatedUserTransformer"/> class.
		/// </summary>
		public UpdatedUserTransformer()
			: base(model => model.User != null
				? new UpdatedUser(model.User)
				: new UpdatedUser(model.Id))
		{
		}
	}
}
