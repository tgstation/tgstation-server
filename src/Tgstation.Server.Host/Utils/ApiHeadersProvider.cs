using System;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tgstation.Server.Api;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class ApiHeadersProvider : IApiHeadersProvider
	{
		/// <inheritdoc />
		public ApiHeaders ApiHeaders { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeadersProvider"/> class.
		/// </summary>
		/// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> for accessing the <see cref="HttpContext"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		public ApiHeadersProvider(IHttpContextAccessor httpContextAccessor, ILogger<ApiHeadersProvider> logger)
		{
			ArgumentNullException.ThrowIfNull(httpContextAccessor);

			if (httpContextAccessor.HttpContext == null)
				throw new InvalidOperationException("httpContextAccessor has no HttpContext!");

			var request = httpContextAccessor.HttpContext.Request;
			try
			{
				ApiHeaders = new ApiHeaders(request.GetTypedHeaders());
			}
			catch (HeadersException ex)
			{
				// we are not responsible for handling header validation issues
				logger.LogTrace(ex, "Failed to validated API request headers!");
			}
		}
	}
}
