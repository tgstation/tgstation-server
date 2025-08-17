using Tgstation.Server.Host.GraphQL.Mutations.Payloads;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="LoginResult"/>s.
	/// </summary>
	sealed class LoginResultTransformer : TransformerBase<string, LoginResult>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LoginResultTransformer"/> class.
		/// </summary>
		public LoginResultTransformer()
			: base(
				token => new LoginResult
				{
					Bearer = token,
				})
		{
		}
	}
}
