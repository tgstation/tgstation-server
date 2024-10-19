namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="GraphQL.Types.UserName"/>s.
	/// </summary>
	sealed class UserNameGraphQLTransformer : TransformerBase<User, GraphQL.Types.UserName>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserNameGraphQLTransformer"/> class.
		/// </summary>
		public UserNameGraphQLTransformer()
			: base(model => new GraphQL.Types.UserName
			{
				Id = model.Id!.Value,
				Name = model.Name!,
			})
		{
		}
	}
}
