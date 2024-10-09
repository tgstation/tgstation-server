using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StrawberryShake;

using Tgstation.Server.Client.GraphQL;

namespace Tgstation.Server.Tests.Live
{
	static class GraphQLServerClientExtensions
	{
		public static async ValueTask<TResultData> RunQueryEnsureNoErrors<TResultData>(
			this IGraphQLServerClient serverClient,
			Func<IGraphQLClient, Task<IOperationResult<TResultData>>> operationExecutor,
			CancellationToken cancellationToken)
			where TResultData : class
		{
			var result = await serverClient.RunOperation(operationExecutor, cancellationToken);
			result.EnsureNoErrors();
			return result.Data;
		}

		public static async ValueTask<TPayload> RunMutationEnsureNoErrors<TResultData, TPayload>(
			this IGraphQLServerClient serverClient,
			Func<IGraphQLClient, Task<IOperationResult<TResultData>>> operationExecutor,
			Func<TResultData, TPayload> payloadSelector,
			CancellationToken cancellationToken)
			where TResultData : class
		{
			var result = await serverClient.RunOperation(operationExecutor, cancellationToken);
			result.EnsureNoErrors();
			var data = payloadSelector(result.Data);
			var errorsObject = data.GetType().GetProperty("Errors").GetValue(data);
			if (errorsObject != null)
			{
				var errorsCount = (int)errorsObject.GetType().GetProperty("Count").GetValue(errorsObject);

				Assert.AreEqual(0, errorsCount);
			}

			return data;
		}
	}
}
