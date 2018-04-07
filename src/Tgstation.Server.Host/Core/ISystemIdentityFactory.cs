using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Core
{
	interface ISystemIdentityFactory
	{
		ISystemIdentity CreateSystemIdentity(User user);
		ISystemIdentity CreateSystemIdentity(string username, string password);
	}
}
