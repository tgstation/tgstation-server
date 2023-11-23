using System;
using System.Collections.Generic;
using System.Text;

using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Rest.Results;
using Remora.Results;

#nullable disable

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
			ArgumentNullException.ThrowIfNull(result);

			if (result.IsSuccess)
				return "SUCCESS?";

			var stringBuilder = new StringBuilder();
			if (result.Error != null)
			{
				stringBuilder.Append(result.Error.Message);
				if (result.Error is RestResultError<RestError> restError)
				{
					stringBuilder.Append(" (");
					if (restError.Error != null)
					{
						stringBuilder.Append(restError.Error.Code);
						stringBuilder.Append(": ");
						stringBuilder.Append(restError.Error.Message);
						stringBuilder.Append('|');
					}

					stringBuilder.Append(restError.Message);
					if ((restError.Error?.Errors.HasValue ?? false) && restError.Error.Errors.Value.Count > 0)
					{
						stringBuilder.Append(" (");
						foreach (var error in restError.Error.Errors.Value)
						{
							stringBuilder.Append(error.Key);
							stringBuilder.Append(':');
							if (error.Value.IsT0)
							{
								FormatErrorDetails(error.Value.AsT0, stringBuilder);
							}
							else
								FormatErrorDetails(error.Value.AsT1, stringBuilder);
							stringBuilder.Append(',');
						}

						stringBuilder.Remove(stringBuilder.Length - 1, 1);
					}

					stringBuilder.Append(')');
				}
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

		/// <summary>
		/// Formats given <paramref name="propertyErrorDetails"/> into a given <paramref name="stringBuilder"/>.
		/// </summary>
		/// <param name="propertyErrorDetails">The <see cref="IPropertyErrorDetails"/>.</param>
		/// <param name="stringBuilder">The <see cref="StringBuilder"/> to mutate.</param>
		static void FormatErrorDetails(IPropertyErrorDetails propertyErrorDetails, StringBuilder stringBuilder)
		{
			if (propertyErrorDetails == null)
				return;

			FormatErrorDetails(propertyErrorDetails.Errors, stringBuilder);

			if (propertyErrorDetails.Errors != null && propertyErrorDetails.MemberErrors != null)
			{
				stringBuilder.Append(',');
			}

			if (propertyErrorDetails.MemberErrors != null)
			{
				stringBuilder.Append('{');
				foreach (var error in propertyErrorDetails.MemberErrors)
				{
					stringBuilder.Append(error.Key);
					stringBuilder.Append(':');
					FormatErrorDetails(error.Value, stringBuilder);
					stringBuilder.Append(',');
				}

				stringBuilder.Remove(stringBuilder.Length - 1, 1);
				stringBuilder.Append('}');
			}
		}

		/// <summary>
		/// Formats given <paramref name="errorDetails"/> into a given <paramref name="stringBuilder"/>.
		/// </summary>
		/// <param name="errorDetails">The <see cref="IEnumerable{T}"/> of <see cref="IErrorDetails"/>.</param>
		/// <param name="stringBuilder">The <see cref="StringBuilder"/> to mutate.</param>
		static void FormatErrorDetails(IEnumerable<IErrorDetails> errorDetails, StringBuilder stringBuilder)
		{
			if (errorDetails == null)
				return;

			stringBuilder.Append('[');
			foreach (var error in errorDetails)
			{
				stringBuilder.Append(error.Code);
				stringBuilder.Append(':');
				stringBuilder.Append(error.Message);
				stringBuilder.Append(',');
			}

			stringBuilder.Remove(stringBuilder.Length - 1, 1);
			stringBuilder.Append(']');
		}
	}
}
