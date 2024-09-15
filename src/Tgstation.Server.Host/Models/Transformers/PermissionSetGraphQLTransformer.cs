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
			: base(model => new GraphQL.Types.PermissionSet
			{
				AdministrationRights = model.AdministrationRights!.Value,
				InstanceManagerRights = model.InstanceManagerRights!.Value,
			})
		{
		}
	}
}
