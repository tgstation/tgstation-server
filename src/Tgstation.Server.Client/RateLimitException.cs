using System;
using System.Linq;
using System.Net.Http;

using Microsoft.Net.Http.Headers;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when a GitHub rate limit occurs.
	/// </summary>
	public sealed class RateLimitException : ApiException
	{
		/// <summary>
		/// Gets the <see cref="DateTimeOffset"/> to try the request again after.
		/// </summary>
		public DateTimeOffset? RetryAfter { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RateLimitException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public RateLimitException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
			if (responseMessage == null)
				throw new ArgumentNullException(nameof(responseMessage));

			if (!responseMessage.Headers.TryGetValues(HeaderNames.RetryAfter, out var values))
				return;

			var secondsString = values.FirstOrDefault();
			if (UInt32.TryParse(secondsString, out var seconds))
				RetryAfter = DateTimeOffset.UtcNow.AddSeconds(seconds);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RateLimitException"/> class.
		/// </summary>
		public RateLimitException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RateLimitException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public RateLimitException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RateLimitException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public RateLimitException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
