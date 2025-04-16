using Grpc.Core;

namespace Tgstation.Server.Host.Swarm.Tests
{
	internal class UnavailableCallInvoker : CallInvoker
	{
		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
			=> throw Exception();

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
			=> throw Exception();

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
			=> throw Exception();

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
			=> throw Exception();

		public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
			=> throw Exception();

		static RpcException Exception()
			=> throw new RpcException(new Status(StatusCode.Unavailable, "Disconnected call invoker"));
	}
}
