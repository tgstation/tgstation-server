using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class TestBridgeHandler : Chunker, IBridgeHandler
	{
		class DMApiParametersImpl : DMApiParameters { }

		class BridgeResponseHack : BridgeResponse
		{
			public string IntegrationHack { get; set; }
		}

		public DMApiParameters DMApiParameters => new DMApiParametersImpl
		{
			AccessIdentifier = "tgs_integration_test"
		};

		long lastBridgeRequestSize = 0;

		readonly TaskCompletionSource bridgeTestsTcs;
		readonly ushort serverPort;

		bool chunksProcessed = false;

		public TestBridgeHandler(TaskCompletionSource tcs, ILogger<TestBridgeHandler> logger, ushort serverPort)
			: base(logger)
		{
			bridgeTestsTcs = tcs;
			this.serverPort = serverPort;
		}

		public async Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			try
			{
				Logger.LogTrace("Bridge request received");
				Assert.AreEqual(DMApiParameters.AccessIdentifier, parameters.AccessIdentifier);
				if (parameters.CommandType == BridgeCommandType.Chunk)
					return await ProcessChunk<BridgeParameters, BridgeResponse>(
						(parameters, cancellationToken) =>
						{
							chunksProcessed = true;
							return ProcessBridgeRequest(parameters, cancellationToken);
						},
						error =>
						{
							bridgeTestsTcs.SetException(new Exception(error));
							return new BridgeResponse
							{
								ErrorMessage = error,
							};
						},
						parameters.Chunk,
						cancellationToken);

				Assert.AreEqual((BridgeCommandType)0, parameters.CommandType);
				Assert.IsNotNull(parameters.ChatMessage?.Text);
				var splits = parameters.ChatMessage.Text.Split(':', StringSplitOptions.RemoveEmptyEntries);
				Assert.AreEqual(2, splits.Length);
				var coreMessage = splits[0];
				Assert.IsFalse(string.IsNullOrWhiteSpace(coreMessage));
				if (coreMessage == "done")
				{
					Assert.IsTrue(chunksProcessed);
					Assert.AreEqual(DMApiConstants.MaximumBridgeRequestLength, lastBridgeRequestSize);
					Assert.AreEqual(new string('a', (int)(DMApiConstants.MaximumBridgeRequestLength * 3)), splits[1]);

					bridgeTestsTcs.SetResult();
					return new BridgeResponseHack
					{
						IntegrationHack = "ok"
					};
				}

				Assert.AreEqual("payload", coreMessage);
				var serializedRequest = JsonConvert.SerializeObject(parameters, DMApiConstants.SerializerSettings);
				var actualLastRequest = $"http://127.0.0.1:{serverPort}/Bridge?data=" + HttpUtility.UrlEncode(serializedRequest);
				lastBridgeRequestSize = actualLastRequest.Length;
				return new BridgeResponseHack
				{
					IntegrationHack = "ok"
				};
			}
			catch (Exception ex)
			{
				bridgeTestsTcs.SetException(ex);
				return null;
			}
		}
	}
}
