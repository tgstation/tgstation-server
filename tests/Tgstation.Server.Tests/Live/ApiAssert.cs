using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StrawberryShake;

using Tgstation.Server.Client;
using Tgstation.Server.Client.GraphQL;

using static HotChocolate.ErrorCodes;

namespace Tgstation.Server.Tests.Live
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
		public static async ValueTask ThrowsExactly<TApiException>(Func<ValueTask> action, Api.Models.ErrorCode? expectedErrorCode = null)
			where TApiException : ApiException
		{
			try
			{
				await action();
			}
			catch (TApiException ex)
			{
				Assert.AreEqual(expectedErrorCode, ex.ErrorCode, $"Wrong error code for expected API exception! Additional Data: {ex.AdditionalServerData}");
				return;
			}

			Assert.Fail($"Expected exception {typeof(TApiException)}!");
		}

		/// <summary>
		/// Test a given <typeparamref name="TApiException"/> is thrown when a given <paramref name="action"/> is called.
		/// </summary>
		/// <typeparam name="TApiException">The type of the expected <see cref="ApiException"/>.</typeparam>
		/// <typeparam name="TResult">The type of the returned <see cref="ValueTask{TResult}.Result"/>.</typeparam>
		/// <param name="action">A <see cref="Func{TResult}"/> resulting in a <see cref="Task"/>.</param>
		/// <param name="expectedErrorCode">The expected <see cref="ApiException.ErrorCode"/>.</param>
		/// <returns>A <see cref="Task"/> representing the running operation,</returns>
		public static async ValueTask ThrowsExactly<TApiException, TResult>(Func<ValueTask<TResult>> action, Api.Models.ErrorCode? expectedErrorCode = null)
			where TApiException : ApiException
		{
			try
			{
				await action();
			}
			catch (TApiException ex)
			{
				Assert.AreEqual(expectedErrorCode, ex.ErrorCode, $"Wrong error code for expected API exception! Additional Data: {ex.AdditionalServerData}");
				return;
			}

			Assert.Fail($"Expected exception {typeof(TApiException)}!");
		}

		public static async ValueTask OperationFails<TResultData, TPayload>(
			IGraphQLServerClient client,
			Func<IGraphQLClient, Task<IOperationResult<TResultData>>> operationInvoker,
			Func<TResultData, TPayload> payloadSelector,
			Client.GraphQL.ErrorCode expectedErrorCode,
			CancellationToken cancellationToken)
			where TResultData : class
		{
			var operationResult = await client.RunOperation(operationInvoker, cancellationToken);
			operationResult.EnsureNoErrors();

			var payload = payloadSelector(operationResult.Data);

			Assert.AreNotSame<object>(operationResult.Data, payload, "Select the mutation payload from the operation result!");
			var payloadErrors = (IEnumerable<object>)payload.GetType().GetProperty("Errors").GetValue(payload);
			var error = payloadErrors.Single();

			var errorCode = (ErrorCode)error.GetType().GetProperty("ErrorCode").GetValue(error);
			Assert.AreEqual(expectedErrorCode, errorCode);
		}
	}
}
