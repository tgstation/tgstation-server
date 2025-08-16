namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="GraphQL.Types.OAuth.OAuthConnection"/>s.
	/// </summary>
	sealed class OAuthConnectionGraphQLTransformer : TransformerBase<OAuthConnection, GraphQL.Types.OAuth.OAuthConnection>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthConnectionGraphQLTransformer"/> class.
		/// </summary>
		public OAuthConnectionGraphQLTransformer()
			: base(model => new GraphQL.Types.OAuth.OAuthConnection
			{
				ExternalUserId = model.ExternalUserId ?? NotNullFallback<string>(),
				Provider = model.Provider,
			})
		{
		}
	}
}
