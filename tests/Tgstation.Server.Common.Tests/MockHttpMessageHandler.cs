using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Common.Tests
{
	/// <summary>
	/// Simple mock <see cref="HttpMessageHandler"/>.
	/// </summary>
	public sealed class MockHttpMessageHandler : HttpMessageHandler
	{
		readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback;

		public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback)
		{
			this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> callback(request, cancellationToken);
	}
}
