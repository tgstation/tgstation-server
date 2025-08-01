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
using Tgstation.Server.Common.Tests;

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

			var handler = new MockHttpMessageHandler((_, __) => Task.FromResult(response));

			var client = new ApiClient(
				new HttpClient(handler),
				new Uri("http://fake.com"),
				new ApiHeaders(
					new ProductHeaderValue("fake"),
					new TokenResponse
					{
						Bearer = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMyIsImV4cCI6IjE2OTkzOTUwNTIiLCJuYmYiOiIxNjk5MzA4NjUyIiwiaXNzIjoiVGdzdGF0aW9uLlNlcnZlci5Ib3N0IiwiYXVkIjoiVGdzdGF0aW9uLlNlcnZlci5BcGkifQ.GRqEd3LRYLkbzk7NHTqcBPX-Xc1vmE_zmbJEDowAXV4",
					}),
				null,
				false);

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

			var handler = new MockHttpMessageHandler((_, __) => Task.FromResult(response));

			var client = new ApiClient(
				new HttpClient(handler),
				new Uri("http://fake.com"),
				new ApiHeaders(
					new ProductHeaderValue("fake"),
					new TokenResponse
					{
						Bearer = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMyIsImV4cCI6IjE2OTkzOTUwNTIiLCJuYmYiOiIxNjk5MzA4NjUyIiwiaXNzIjoiVGdzdGF0aW9uLlNlcnZlci5Ib3N0IiwiYXVkIjoiVGdzdGF0aW9uLlNlcnZlci5BcGkifQ.GRqEd3LRYLkbzk7NHTqcBPX-Xc1vmE_zmbJEDowAXV4"
					}),
				null,
				false);

			await Assert.ThrowsExceptionAsync<UnrecognizedResponseException>(() => client.Read<EngineResponse>(Routes.Engine, default).AsTask());
		}
	}
}
