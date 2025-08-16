using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="PermissionSet"/>s.
	/// </summary>
	sealed class PermissionSetTransformer : Models.TransformerBase<Models.PermissionSet, PermissionSet>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionSetTransformer"/> class.
		/// </summary>
		public PermissionSetTransformer()
			: base(
				  BuildSubProjection<
					  InstanceManagerRights?,
					  AdministrationRights?,
					  RightsHolder<InstanceManagerRights>,
					  RightsHolder<AdministrationRights>,
					  RightsHolderTransformer<InstanceManagerRights>,
					  RightsHolderTransformer<AdministrationRights>>(
				  (model, instanceManagerRights, administrationRights) => new PermissionSet
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
