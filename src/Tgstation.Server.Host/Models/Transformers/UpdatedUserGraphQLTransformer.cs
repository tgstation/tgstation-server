namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="UpdatedUser"/>s.
	/// </summary>
	sealed class UpdatedUserGraphQLTransformer : TransformerBase<UpdatedUser, GraphQL.Types.UpdatedUser>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UpdatedUserGraphQLTransformer"/> class.
		/// </summary>
		public UpdatedUserGraphQLTransformer()
			: base(model => model.User != null
				? new GraphQL.Types.UpdatedUser(model.User)
				: new GraphQL.Types.UpdatedUser(model.Id))
		{
		}
	}
}
