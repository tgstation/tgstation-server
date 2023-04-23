using Tgstation.Server.Common;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class ConcreteHttpClientFactory : IAbstractHttpClientFactory
	{
		public IHttpClient CreateClient() => new HttpClient();
	}
}
