using System;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Core.Testing;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Host.Swarm.Grpc;

namespace Tgstation.Server.Host.Swarm.Tests
{
	sealed class SwarmMockCallInvoker : CallInvoker
	{
		readonly Func<SwarmControllerService> controllerService;
		readonly Func<SwarmNodeService> nodeService;
		readonly Func<SwarmSharedService> sharedService;

		readonly Func<bool> throwUnavailableException;

		readonly ILogger logger;

		public SwarmMockCallInvoker(Func<SwarmControllerService> controllerService, Func<SwarmSharedService> sharedService, Func<bool> throwUnavailableException, ILogger logger)
		{
			this.controllerService = controllerService ?? throw new ArgumentNullException(nameof(controllerService));
			this.sharedService = sharedService ?? throw new ArgumentNullException(nameof(sharedService));
			this.throwUnavailableException = throwUnavailableException ?? throw new ArgumentNullException(nameof(throwUnavailableException));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public SwarmMockCallInvoker(Func<SwarmNodeService> nodeService, Func<SwarmSharedService> sharedService, Func<bool> throwUnavailableException, ILogger logger)
		{
			this.nodeService = nodeService ?? throw new ArgumentNullException(nameof(nodeService));
			this.sharedService = sharedService ?? throw new ArgumentNullException(nameof(sharedService));
			this.throwUnavailableException = throwUnavailableException ?? throw new ArgumentNullException(nameof(throwUnavailableException));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
			=> throw new NotSupportedException();

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
			=> throw new NotSupportedException();

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
			=> throw new NotSupportedException();

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
		{
			var serviceName = method.ServiceName;

			var headersResult = new TaskCompletionSource<Metadata>();

			var callContext = TestServerCallContext.Create(
				serviceName,
				"doesn't matter",
				options.Deadline ?? default,
				options.Headers,
				options.CancellationToken,
				"also doesn't matter",
				null,
				null,
				headers =>
				{
					headersResult.SetResult(headers);
					return Task.CompletedTask;
				},
				null,
				null);

			Func<object> getInvokeTarget;
			if (serviceName == GrpcSwarmControllerService.Descriptor.FullName)
				getInvokeTarget = controllerService;
			else if (serviceName == GrpcSwarmNodeService.Descriptor.FullName)
				getInvokeTarget = nodeService;
			else if (serviceName == GrpcSwarmSharedService.Descriptor.FullName)
				getInvokeTarget = sharedService;
			else
			{
				Assert.Fail($"Unsupported gRPC method call: {serviceName}");
				getInvokeTarget = null;
			}

			if (getInvokeTarget == null)
				Assert.Fail($"Call to {serviceName} not targeting the correct service!");

			logger.LogTrace("Attempting mock gRPC call to {fullCall}", method.FullName);

			if (throwUnavailableException())
				throw new RpcException(new Status(StatusCode.Unavailable, "Testing machine said so"));

			var invokeTarget = getInvokeTarget();
			if (invokeTarget == null)
				throw new RpcException(new Status(StatusCode.Unavailable, "Invoke target is down"));

			var responseTask = (Task<TResponse>)invokeTarget.GetType().GetMethod(method.Name).Invoke(invokeTarget, [request, callContext]);

			var call = TestCalls.AsyncUnaryCall(responseTask, headersResult.Task, () => callContext.Status, () => callContext.ResponseTrailers, () => { });

			return call;
		}

		public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
			=> throw new NotSupportedException();
	}
}
