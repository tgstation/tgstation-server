using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Controllers.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="UserName"/>s.
	/// </summary>
	sealed class UserNameTransformer : TransformerBase<User, UserName>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserNameTransformer"/> class.
		/// </summary>
		public UserNameTransformer()
			: base(
				  model => new UserName
				  {
					  Id = model.Id,
					  Name = model.Name,
				  })
		{
		}
	}
}
