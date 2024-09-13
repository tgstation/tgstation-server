using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace Tgstation.Server.Host.GraphQL.Types.Interceptors
{
	/// <summary>
	/// Makes the @authorize directive public (It is internal by default).
	/// </summary>
	public sealed class PublicAuthorizeDirectiveTypeInterceptor : TypeInterceptor
	{
		/// <inheritdoc />
		public override void OnBeforeRegisterDependencies(ITypeDiscoveryContext discoveryContext, DefinitionBase definition)
		{
			if (definition is DirectiveTypeDefinition dtd
				&& dtd.Name == "authorize")
			{
				dtd.IsPublic = true;
			}

			base.OnBeforeRegisterDependencies(discoveryContext, definition);
		}
	}
}
