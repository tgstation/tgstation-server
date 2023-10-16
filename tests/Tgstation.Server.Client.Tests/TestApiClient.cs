using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common.Http;

namespace Tgstation.Server.Client.Tests
{
	[TestClass]
	public sealed class TestApiClient
	{
		[TestMethod]
		public async Task TestDeserializingByondModelsWork()
		{
			var sample = new EngineResponse
			{
				EngineVersion = new EngineVersion
				{
					Engine = EngineType.Byond,
					Version = new Version(511, 1385)
				}
			};

			var sampleJson = JsonConvert.SerializeObject(sample, new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				Converters = new[] { new VersionConverter() }
			});

			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(sampleJson)
			};

			var httpClient = new Mock<IHttpClient>();
			httpClient.Setup(x => x.SendAsync(It.IsNotNull<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

			var client = new ApiClient(httpClient.Object, new Uri("http://fake.com"), new ApiHeaders(new ProductHeaderValue("fake"), "fake"), null, false);

			var result = await client.Read<EngineResponse>(Routes.Engine, default);
			Assert.AreEqual(sample.EngineVersion, result.EngineVersion);
			Assert.AreEqual(-1, result.EngineVersion.Version.Build);
			Assert.IsFalse(result.EngineVersion.CustomIteration.HasValue);
		}

		[TestMethod]
		public async Task TestUnrecognizedResponse()
		{
			var sample = new EngineResponse
			{
				EngineVersion = new EngineVersion
				{
					Engine = EngineType.Byond,
					Version = new Version(511, 1385)
				}
			};

			var fakeJson = "asdfasd <>F#(*)U*#JLI";

			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(fakeJson)
			};

			var httpClient = new Mock<IHttpClient>();
			httpClient.Setup(x => x.SendAsync(It.IsNotNull<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(response));

			var client = new ApiClient(httpClient.Object, new Uri("http://fake.com"), new ApiHeaders(new ProductHeaderValue("fake"), "fake"), null, true);

			await Assert.ThrowsExceptionAsync<UnrecognizedResponseException>(() => client.Read<EngineResponse>(Routes.Engine, default).AsTask());
		}
	}
}
