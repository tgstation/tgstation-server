using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="UserName"/>s.
	/// </summary>
	sealed class UserNameTransformer : Models.TransformerBase<Models.User, UserName>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserNameTransformer"/> class.
		/// </summary>
		public UserNameTransformer()
			: base(model => new UserName
			{
				Id = model.Id!.Value,
				Name = model.Name!,
			})
		{
		}
	}
}
