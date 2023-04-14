using System;
using System.Text;

using Remora.Results;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for <see cref="IResult"/>.
	/// </summary>
	static class ResultExtensions
	{
		/// <summary>
		/// Converts a given <paramref name="result"/> into a log entry <see cref="string"/>.
		/// </summary>
		/// <param name="result">The <see cref="IResult"/> to convert.</param>
		/// <param name="level">Used internally for nesting.</param>
		/// <returns>The <see cref="string"/> formatted <paramref name="result"/>.</returns>
		public static string LogFormat(this IResult result, uint level = 0)
		{
			if (result == null)
				throw new ArgumentNullException(nameof(result));

			if (result.IsSuccess)
				return "SUCCESS?";

			var stringBuilder = new StringBuilder();
			if (result.Error != null)
			{
				stringBuilder.Append(result.Error.Message);
			}

			if (result.Inner != null)
			{
				stringBuilder.Append(Environment.NewLine);
				++level;
				for (var i = 0; i < level; ++i)
					stringBuilder.Append('\t');
				stringBuilder.Append(result.Inner.LogFormat(level));
			}

			return stringBuilder.ToString();
		}
	}
}
