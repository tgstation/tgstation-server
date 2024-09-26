using System;
using System.Linq;

using StrawberryShake;

namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// Extension methods for the <see cref="IOperationResult"/> interface.
	/// </summary>
	public static class OperationResultExtensions
	{
		/// <summary>
		/// Checks if a given <paramref name="operationResult"/> errored out with authentication errors.
		/// </summary>
		/// <param name="operationResult">The <see cref="IOperationResult"/>.</param>
		/// <returns><see langword="true"/> if <paramref name="operationResult"/> errored due to authentication issues, <see langword="false"/> otherwise.</returns>
		public static bool IsAuthenticationError(this IOperationResult operationResult)
		{
			ArgumentNullException.ThrowIfNull(operationResult);

			return operationResult.Errors.Any(
				error => error.Extensions?.TryGetValue(
					"code",
					out object? codeExtension) == true
				&& codeExtension is string codeExtensionString
				&& codeExtensionString == "AUTH_NOT_AUTHENTICATED");
		}
	}
}
