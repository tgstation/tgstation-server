using System;

using Tgstation.Server.Host.GraphQL.Types;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="RightsHolder{TRight}"/>s.
	/// </summary>
	/// <typeparam name="TRight">The <see cref="Api.Rights.RightsType"/> of the <see cref="RightsHolder{TRight}"/>.</typeparam>
	sealed class RightsHolderTransformer<TRight> : TransformerBase<TRight?, RightsHolder<TRight>>
		where TRight : struct, Enum
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RightsHolderTransformer{TRight}"/> class.
		/// </summary>
		public RightsHolderTransformer()
			: base(right => new RightsHolder<TRight>
			{
				Right = right ?? NotNullFallback<TRight>(),
			})
		{
		}
	}
}
