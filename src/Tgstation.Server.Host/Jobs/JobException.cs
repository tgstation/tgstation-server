using System;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Operation exceptions thrown from the context of a <see cref="Models.Job"/>
	/// </summary>
	public sealed class JobException : Exception
	{
		/// <summary>
		/// The <see cref="Api.Models.ErrorCode"/> associated with the <see cref="JobException"/>.
		/// </summary>
		public ErrorCode? ErrorCode { get; set; }

		/// <summary>
		/// Construct a <see cref="JobException"/>
		/// </summary>
		public JobException()
		{
		}

		/// <summary>
		/// Construct a <see cref="JobException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public JobException(string message) : base(message)
		{
		}

		/// <summary>
		/// Construct a <see cref="JobException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the nase <see cref="Exception"/></param>
		public JobException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Construct a <see cref="JobException"/> with a <paramref name="errorCode"/>.
		/// </summary>
		/// <param name="errorCode">The associated <see cref="Api.Models.ErrorCode"/>.</param>
		public JobException(ErrorCode errorCode) : base(errorCode.Describe())
		{
			ErrorCode = errorCode;
		}

		/// <summary>
		/// Construct a <see cref="JobException"/> with a <paramref name="errorCode"/> and <paramref name="innerException"/>.
		/// </summary>
		/// <param name="errorCode">The associated <see cref="Api.Models.ErrorCode"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the nase <see cref="Exception"/></param>
		public JobException(ErrorCode errorCode, Exception innerException) : base(errorCode.Describe(), innerException)
		{
			ErrorCode = errorCode;
		}
	}
}
