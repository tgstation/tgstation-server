using System;

using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.Models.Transformers
{
	sealed class RightsHolderGraphQLTransformer<TRight> : TransformerBase<TRight?, RightsHolder<TRight>>
		where TRight : struct, Enum
	{
		public RightsHolderGraphQLTransformer()
			: base(right => new RightsHolder<TRight>
			{
				Right = right ?? NotNullFallback<TRight>(),
			})
		{
		}
	}
}
