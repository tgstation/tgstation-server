using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Controllers.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="TokenResponse"/>s.
	/// </summary>
	sealed class TokenResponseTransformer : TransformerBase<string, TokenResponse>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TokenResponseTransformer"/> class.
		/// </summary>
		public TokenResponseTransformer()
			: base(
				token => new TokenResponse
				{
					Bearer = token,
				})
		{
		}
	}
}
