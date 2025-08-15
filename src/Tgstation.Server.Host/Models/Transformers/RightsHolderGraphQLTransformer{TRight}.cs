using System;

using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="RightsHolder{TRight}"/>s.
	/// </summary>
	/// <typeparam name="TRight">The <see cref="Api.Rights.RightsType"/> of the <see cref="RightsHolder{TRight}"/>.</typeparam>
	sealed class RightsHolderGraphQLTransformer<TRight> : TransformerBase<TRight?, RightsHolder<TRight>>
		where TRight : struct, Enum
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RightsHolderGraphQLTransformer{TRight}"/> class.
		/// </summary>
		public RightsHolderGraphQLTransformer()
			: base(right => new RightsHolder<TRight>
			{
				Right = right ?? NotNullFallback<TRight>(),
			})
		{
		}
	}
}
