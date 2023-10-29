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
		public ApiHeaders ApiHeaders => attemptedApiHeadersCreation
			? apiHeaders
			: CreateApiHeaders(true);

		/// <inheritdoc />
		public HeadersException HeadersException { get; private set; }

		/// <summary>
		/// The <see cref="IHttpContextAccessor"/> for the <see cref="ApiHeadersProvider"/>.
		/// </summary>
		readonly IHttpContextAccessor httpContextAccessor;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ApiHeadersProvider"/>.
		/// </summary>
		readonly ILogger<ApiHeadersProvider> logger;

		/// <summary>
		/// Backing field for <see cref="ApiHeaders"/>.
		/// </summary>
		ApiHeaders apiHeaders;

		/// <summary>
		/// If populating <see cref="ApiHeaders"/> was previously attempted.
		/// </summary>
		bool attemptedApiHeadersCreation;

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeadersProvider"/> class.
		/// </summary>
		/// <param name="httpContextAccessor">The value of <see cref="httpContextAccessor"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public ApiHeadersProvider(IHttpContextAccessor httpContextAccessor, ILogger<ApiHeadersProvider> logger)
		{
			this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public ApiHeaders CreateAuthlessHeaders() => CreateApiHeaders(false);

		/// <summary>
		/// Attempt to parse <see cref="Api.ApiHeaders"/> from the <see cref="HttpContext"/>, optionally populating the <see langword="class"/> properties.
		/// </summary>
		/// <param name="includeAuthAndSetProperties">If the <see cref="HeaderErrorTypes.AuthorizationMissing"/> error should be ignored and <see cref="ApiHeaders"/>/<see cref="HeadersException"/> should be populated.</param>
		/// <returns>A newly parsed <see cref="Api.ApiHeaders"/> <see langword="class"/> or <see langword="null"/> if <paramref name="includeAuthAndSetProperties"/> was set and the parse failed.</returns>
		ApiHeaders CreateApiHeaders(bool includeAuthAndSetProperties)
		{
			if (httpContextAccessor.HttpContext == null)
				throw new InvalidOperationException("httpContextAccessor has no HttpContext!");

			var request = httpContextAccessor.HttpContext.Request;
			var ignoreMissingAuth = !includeAuthAndSetProperties;

			if (includeAuthAndSetProperties)
				attemptedApiHeadersCreation = true;

			try
			{
				var headers = new ApiHeaders(request.GetTypedHeaders(), ignoreMissingAuth);
				if (includeAuthAndSetProperties)
					apiHeaders = headers;

				return headers;
			}
			catch (HeadersException ex) when (includeAuthAndSetProperties)
			{
				logger.LogTrace(ex, "Failed to parse API headers!");
				HeadersException = ex;
				return null;
			}
		}
	}
}
