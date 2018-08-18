using System;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Operation exceptions thrown from the context of a <see cref="Models.Job"/>
	/// </summary>
	public sealed class JobException : Exception
	{
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
	}
}
