namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="GraphQL.Types.UserGroup"/>s.
	/// </summary>
	sealed class UserGroupGraphQLTransformer : TransformerBase<UserGroup, GraphQL.Types.UserGroup>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupGraphQLTransformer"/> class.
		/// </summary>
		public UserGroupGraphQLTransformer()
			: base(model => new GraphQL.Types.UserGroup
			{
				Id = model.Id!.Value,
				Name = model.Name!,
			})
		{
		}
	}
}
