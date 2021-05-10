using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;

namespace Tgstation.Server.Tests
{
	/// <summary>
	/// Extension methods for the <see cref="Assert"/> <see langword="ckass"/>.
	/// </summary>
	static class ApiAssert
	{
		/// <summary>
		/// Test a given <typeparamref name="TApiException"/> is thrown when a given <paramref name="action"/> is called.
		/// </summary>
		/// <typeparam name="TApiException">The type of the expected <see cref="ApiException"/>.</typeparam>
		/// <param name="action">A <see cref="Func{TResult}"/> resulting in a <see cref="Task"/>.</param>
		/// <param name="expectedErrorCode">The expected <see cref="ApiException.ErrorCode"/>.</param>
		/// <returns>A <see cref="Task"/> representing the running operation,</returns>
		public static async Task ThrowsException<TApiException>(Func<Task> action, ErrorCode? expectedErrorCode)
			where TApiException : ApiException
		{
			try
			{
				await action().ConfigureAwait(false);
			}
			catch (TApiException ex)
			{
				Assert.AreEqual(expectedErrorCode, ex.ErrorCode, $"Wrong error code for expected API exception! Additional Data: {ex.AdditionalServerData}");
				return;
			}

			Assert.Fail($"Expected exception {typeof(TApiException)}!");
		}
	}
}
