using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Tests
{
	[TestClass]
	public sealed class TestApiClient
	{
		[TestMethod]
		public async Task TestDeserializingByondModelsWork()
		{
			var sample = new Byond
			{
				Version = new Version(511, 1385)
			};

			var sampleJson = JsonConvert.SerializeObject(sample);

			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(sampleJson)
			};

			var httpClient = new Mock<IHttpClient>();
			httpClient.Setup(x => x.SendAsync(It.IsNotNull<HttpRequestMessage>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

			var client = new ApiClient(httpClient.Object, new Uri("http://fake.com"), new ApiHeaders(new ProductHeaderValue("fake"), "fake"));

			var result = await client.Read<Byond>(Routes.Byond, default).ConfigureAwait(false);
			Assert.AreEqual(sample.Version, result.Version);
		}
	}
}
