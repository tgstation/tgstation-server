namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="GraphQL.Types.OAuth.OidcConnection"/>s.
	/// </summary>
	sealed class OidcConnectionGraphQLTransformer : TransformerBase<OidcConnection, GraphQL.Types.OAuth.OidcConnection>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OidcConnectionGraphQLTransformer"/> class.
		/// </summary>
		public OidcConnectionGraphQLTransformer()
			: base(model => new GraphQL.Types.OAuth.OidcConnection
			{
				ExternalUserId = model.ExternalUserId ?? NotNullFallback<string>(),
				SchemeKey = model.SchemeKey ?? NotNullFallback<string>(),
			})
		{
		}
	}
}
