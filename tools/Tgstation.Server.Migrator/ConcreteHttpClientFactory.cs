using Tgstation.Server.Common;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Migrator
{
	sealed class ConcreteHttpClientFactory : IAbstractHttpClientFactory
	{
		public IHttpClient CreateClient() => new HttpClient();
	}
}
