using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components.Tests
{
	[TestClass]
	public sealed class TestDreamDaemonClient
	{
		[TestMethod]
		public async Task TestStart()
		{
			var example = new JobResponse
			{
				Id = 347,
				StartedAt = DateTimeOffset.UtcNow
			};

			var inst = new InstanceResponse
			{
				Id = 4958
			};

			var mockApiClient = new Mock<IApiClient>();
			mockApiClient.Setup(x => x.Create<JobResponse>(Routes.DreamDaemon, inst.Id.Value, It.IsAny<CancellationToken>())).Returns(Task.FromResult(example));

			var client = new DreamDaemonClient(mockApiClient.Object, inst);

			var result = await client.Start(default).ConfigureAwait(false);
			Assert.AreSame(example, result);
		}
	}
}
