using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Web interface for the API
	/// </summary>
	interface IApiClient : IDisposable
	{
		ApiHeaders Headers { get; }

		Uri Url { get; }

		TimeSpan Timeout { get; set; }

		Task<TResult> Create<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken);
		Task<TResult> Create<TResult>(string route, CancellationToken cancellationToken);
		Task<TResult> Read<TResult>(string route, CancellationToken cancellationToken);
		Task<TResult> Update<TBody, TResult>(string route, TBody body, CancellationToken cancellationToken);
		Task<TResult> Update<TResult>(string route, CancellationToken cancellationToken);
		Task Update<TBody>(string route, TBody body, CancellationToken cancellationToken);
		Task Delete(string route, CancellationToken cancellationToken);

		Task<TResult> Create<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken);
		Task<TResult> Create<TResult>(string route, long instanceId, CancellationToken cancellationToken);
		Task<TResult> Read<TResult>(string route, long instanceId, CancellationToken cancellationToken);
		Task<TResult> Update<TBody, TResult>(string route, TBody body, long instanceId, CancellationToken cancellationToken);
		Task Delete(string route, long instanceId, CancellationToken cancellationToken);
	}
}
