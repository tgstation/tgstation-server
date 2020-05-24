using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Net.Http;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when a GitHub rate limit occurs
	/// </summary>
	public sealed class RateLimitException : ApiException
	{
		/// <summary>
		/// Gets the <see cref="DateTimeOffset"/> to try the request again after.
		/// </summary>
		public DateTimeOffset? RetryAfter { get; }

		/// <summary>
		/// Initialize a new instance of the <see cref="MethodNotSupportedException"/> <see langword="class"/>.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public RateLimitException(ErrorMessage? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
			if (!responseMessage.Headers.TryGetValues(HeaderNames.RetryAfter, out var values))
				return;

			var secondsString = values.FirstOrDefault();
			if (UInt32.TryParse(secondsString, out var seconds))
				RetryAfter = DateTimeOffset.Now.AddSeconds(seconds);
		}

		/// <summary>
		/// Construct an <see cref="RateLimitException"/>
		/// </summary>
		public RateLimitException() { }

		/// <summary>
		/// Construct an <see cref="RateLimitException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public RateLimitException(string message, Exception innerException) : base(message, innerException) { }
	}
}