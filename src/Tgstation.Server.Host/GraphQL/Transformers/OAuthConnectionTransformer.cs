using Tgstation.Server.Host.GraphQL.Types.OAuth;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="OAuthConnection"/>s.
	/// </summary>
	sealed class OAuthConnectionTransformer : Models.TransformerBase<Models.OAuthConnection, OAuthConnection>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthConnectionTransformer"/> class.
		/// </summary>
		public OAuthConnectionTransformer()
			: base(model => new Types.OAuth.OAuthConnection
			{
				ExternalUserId = model.ExternalUserId ?? NotNullFallback<string>(),
				Provider = model.Provider,
			})
		{
		}
	}
}
