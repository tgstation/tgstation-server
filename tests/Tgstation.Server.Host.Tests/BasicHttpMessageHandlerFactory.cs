using System;
using System.Net.Http;

namespace Tgstation.Server.Host.Tests
{
	/// <summary>
	/// Basic <see cref="IHttpMessageHandlerFactory"/> implementation for testiong
	/// </summary>
	public sealed class BasicHttpMessageHandlerFactory : IHttpMessageHandlerFactory, IDisposable
	{
		readonly HttpClientHandler handler = new();

		public HttpMessageHandler CreateHandler(string name)
			=> handler;

		public void Dispose()
			=> handler.Dispose();
	}
}
