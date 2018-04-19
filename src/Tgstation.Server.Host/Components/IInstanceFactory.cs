using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	interface IInstanceFactory
	{
		IInstance CreateInstance(Instance metadata);
	}
}