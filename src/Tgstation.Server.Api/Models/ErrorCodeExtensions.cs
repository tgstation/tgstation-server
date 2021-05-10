using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Extension methods for the <see cref="ErrorCode"/> <see langword="enum"/>.
	/// </summary>
	public static class ErrorCodeExtensions
	{
		/// <summary>
		/// Describe a given <paramref name="errorCode"/>.
		/// </summary>
		/// <param name="errorCode">The <see cref="ErrorCode"/> to describe.</param>
		/// <returns>A description of the <paramref name="errorCode"/> on success, <see langword="null"/> on failure.</returns>
		public static string? Describe(this ErrorCode errorCode)
		{
			var attributes = (IEnumerable<DescriptionAttribute>?)typeof(ErrorCode)
			   .GetField(errorCode.ToString())
			   ?.GetCustomAttributes(typeof(DescriptionAttribute), false);

			return attributes?.FirstOrDefault()?.Description;
		}
	}
}
