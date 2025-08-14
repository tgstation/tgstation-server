using System;

namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="GraphQL.Types.User"/>s.
	/// </summary>
	sealed class UserGraphQLTransformer : TransformerBase<User, GraphQL.Types.User>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserGraphQLTransformer"/> class.
		/// </summary>
		public UserGraphQLTransformer()
			: base(model => new GraphQL.Types.User
			{
				CreatedAt = model.CreatedAt ?? NotNullFallback<DateTimeOffset>(),
				CanonicalName = model.CanonicalName ?? NotNullFallback<string>(),
				CreatedById = model.CreatedById,
				Enabled = model.Enabled ?? NotNullFallback<bool>(),
				GroupId = model.GroupId,
				Id = model.Id!.Value,
				Name = model.Name ?? NotNullFallback<string>(),
				SystemIdentifier = model.SystemIdentifier,
			})
		{
		}
	}
}
