using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IInstance : ITGInstance, ITGConnectivity, IInstanceLogger, IRepoConfigProvider
	{
	}
}
