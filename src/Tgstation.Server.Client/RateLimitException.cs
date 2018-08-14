using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when a GitHub rate limit occurs
	/// </summary>
	public sealed class RateLimitException : ClientException
	{
		DateTimeOffset RetryAfter { get; }

		/// <summary>
		/// Construct an <see cref="RateLimitException"/>
		/// </summary>
		public RateLimitException() { }

		/// <summary>
		/// Construct an <see cref="RateLimitException"/> with a <paramref name="secondsString"/>
		/// </summary>
		/// <param name="secondsString">The message for the <see cref="Exception"/></param>
		public RateLimitException(string secondsString) : base(new ErrorMessage
		{
			Message = "GitHub rate limit reached!",
			SeverApiVersion = null
		}, (HttpStatusCode)429)
		{
			if (Int32.TryParse(secondsString, out var seconds) && seconds >= 0)
				RetryAfter = DateTimeOffset.Now.AddSeconds(seconds);
			else
				RetryAfter = DateTimeOffset.Now;
		}

		/// <summary>
		/// Construct an <see cref="RateLimitException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public RateLimitException(string message, Exception innerException) : base(message, innerException) { }
	}
}