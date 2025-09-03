using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="GraphQL.Types.PermissionSet"/>s.
	/// </summary>
	sealed class PermissionSetGraphQLTransformer : TransformerBase<PermissionSet, GraphQL.Types.PermissionSet>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionSetGraphQLTransformer"/> class.
		/// </summary>
		public PermissionSetGraphQLTransformer()
			: base(
				  BuildSubProjection<
					  InstanceManagerRights?,
					  AdministrationRights?,
					  RightsHolder<InstanceManagerRights>,
					  RightsHolder<AdministrationRights>,
					  RightsHolderGraphQLTransformer<InstanceManagerRights>,
					  RightsHolderGraphQLTransformer<AdministrationRights>>(
				  (model, instanceManagerRights, administrationRights) => new GraphQL.Types.PermissionSet
				  {
					  AdministrationRights = administrationRights ?? NotNullFallback<RightsHolder<AdministrationRights>>(),
					  InstanceManagerRights = instanceManagerRights ?? NotNullFallback<RightsHolder<InstanceManagerRights>>(),
				  },
				  model => model.InstanceManagerRights ?? NotNullFallback<InstanceManagerRights>(),
				  model => model.AdministrationRights ?? NotNullFallback<AdministrationRights>()))
		{
		}
	}
}
