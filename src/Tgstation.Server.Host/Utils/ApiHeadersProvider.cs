using System;

using Microsoft.AspNetCore.Http;

using Tgstation.Server.Api;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class ApiHeadersProvider : IApiHeadersProvider
	{
		/// <inheritdoc />
		public ApiHeaders? ApiHeaders => attemptedApiHeadersCreation
			? apiHeaders
			: CreateApiHeaders(false);

		/// <inheritdoc />
		public HeadersException? HeadersException { get; private set; }

		/// <summary>
		/// The <see cref="IHttpContextAccessor"/> for the <see cref="ApiHeadersProvider"/>.
		/// </summary>
		readonly IHttpContextAccessor httpContextAccessor;

		/// <summary>
		/// Backing field for <see cref="ApiHeaders"/>.
		/// </summary>
		ApiHeaders? apiHeaders;

		/// <summary>
		/// If populating <see cref="ApiHeaders"/> was previously attempted.
		/// </summary>
		bool attemptedApiHeadersCreation;

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiHeadersProvider"/> class.
		/// </summary>
		/// <param name="httpContextAccessor">The value of <see cref="httpContextAccessor"/>.</param>
		public ApiHeadersProvider(IHttpContextAccessor httpContextAccessor)
		{
			this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
		}

		/// <inheritdoc />
		public ApiHeaders CreateAuthlessHeaders() => CreateApiHeaders(true)!;

		/// <summary>
		/// Attempt to parse <see cref="Api.ApiHeaders"/> from the <see cref="HttpContext"/>, optionally populating the <see langword="class"/> properties.
		/// </summary>
		/// <param name="authless">If the <see cref="HeaderErrorTypes.AuthorizationMissing"/> error should be ignored and <see cref="ApiHeaders"/>/<see cref="HeadersException"/> should not be populated.</param>
		/// <returns>A newly parsed <see cref="Api.ApiHeaders"/> <see langword="class"/> or <see langword="null"/> if <paramref name="authless"/> was set and the parse failed.</returns>
		ApiHeaders? CreateApiHeaders(bool authless)
		{
			if (httpContextAccessor.HttpContext == null)
				throw new InvalidOperationException("httpContextAccessor has no HttpContext!");

			var typedHeaders = httpContextAccessor.HttpContext.Request.GetTypedHeaders();
			if (!authless)
				attemptedApiHeadersCreation = true;

			try
			{
				var headers = new ApiHeaders(typedHeaders, authless, !authless);
				if (!authless)
					apiHeaders = headers;

				return headers;
			}
			catch (HeadersException ex) when (!authless)
			{
				HeadersException = ex;
				return null;
			}
		}
	}
}
