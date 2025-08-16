using Tgstation.Server.Host.GraphQL.Types.OAuth;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="OidcConnection"/>s.
	/// </summary>
	sealed class OidcConnectionTransformer : Models.TransformerBase<Models.OidcConnection, OidcConnection>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OidcConnectionTransformer"/> class.
		/// </summary>
		public OidcConnectionTransformer()
			: base(model => new OidcConnection
			{
				ExternalUserId = model.ExternalUserId ?? NotNullFallback<string>(),
				SchemeKey = model.SchemeKey ?? NotNullFallback<string>(),
			})
		{
		}
	}
}
