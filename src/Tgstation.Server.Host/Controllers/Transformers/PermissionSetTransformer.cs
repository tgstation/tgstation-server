using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Controllers.Transformers
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
				  model => new PermissionSet
				  {
					  Id = model.Id,
					  AdministrationRights = model.AdministrationRights ?? NotNullFallback<AdministrationRights>(),
					  InstanceManagerRights = model.InstanceManagerRights ?? NotNullFallback<InstanceManagerRights>(),
				  })
		{
		}
	}
}
