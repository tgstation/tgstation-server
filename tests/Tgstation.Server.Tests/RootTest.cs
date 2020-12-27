using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Host;

namespace Tgstation.Server.Tests
{
	class RootTest
	{
		async Task TestRequestValidation(IServerClient serverClient, CancellationToken cancellationToken)
		{
			var url = serverClient.Url;
			var token = serverClient.Token.Bearer;
			// check that 400s are returned appropriately
			using var httpClient = new HttpClient();
			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString()))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.NotAcceptable, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString()))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Xml));
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.NotAcceptable, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString()))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(ErrorCode.BadHeaders, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString()))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ServerInformation>(content);
				Assert.AreEqual(ApiHeaders.Version, message.ApiVersion);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString()))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/6.0.0");
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.UpgradeRequired, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(ErrorCode.ApiMismatch, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.Administration.Substring(1)))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/6.0.0");
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.UpgradeRequired, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(ErrorCode.ApiMismatch, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString() + Routes.Administration.Substring(1)))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				request.Content = new StringContent(
					"{ newVersion: 1234 }",
					Encoding.UTF8,
					MediaTypeNames.Application.Json);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(ErrorCode.ModelValidationFailure, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString() + Routes.DreamDaemon.Substring(1)))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString() + Routes.DreamDaemon.Substring(1)))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(ErrorCode.InstanceHeaderRequired, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString()))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.OAuthAuthenticationScheme, token);
				request.Headers.Add(ApiHeaders.OAuthProviderHeader, "FakeProvider");
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(ErrorCode.BadHeaders, message.ErrorCode);
			}
		}

		async Task TestServerInformation(IServerClientFactory clientFactory, IServerClient serverClient, CancellationToken cancellationToken)
		{
			var serverInfo = await serverClient.ServerInformation(default).ConfigureAwait(false);

			Assert.AreEqual(ApiHeaders.Version, serverInfo.ApiVersion);
			var assemblyVersion = typeof(IServer).Assembly.GetName().Version.Semver();
			Assert.AreEqual(assemblyVersion, serverInfo.Version);
			Assert.AreEqual(10U, serverInfo.MinimumPasswordLength);
			Assert.AreEqual(11U, serverInfo.InstanceLimit);
			Assert.AreEqual(150U, serverInfo.UserLimit);
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), serverInfo.WindowsHost);

			//check that modifying the token even slightly fucks up the auth
			var newToken = new Token
			{
				ExpiresAt = serverClient.Token.ExpiresAt,
				Bearer = serverClient.Token.Bearer + '0'
			};

			var badClient = clientFactory.CreateFromToken(serverClient.Url, newToken);
			await Assert.ThrowsExceptionAsync<UnauthorizedException>(() => badClient.Administration.Read(cancellationToken)).ConfigureAwait(false);
		}

		async Task TestOAuthFails(IServerClient serverClient, CancellationToken cancellationToken)
		{
			var url = serverClient.Url;
			var token = serverClient.Token.Bearer;
			// check that 400s are returned appropriately
			using var httpClient = new HttpClient();

			// just hitting each type of oauth provider for coverage
			foreach (var I in Enum.GetValues(typeof(OAuthProvider)))
				using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString()))
				{
					request.Headers.Accept.Clear();
					request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
					request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
					request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
					request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.OAuthAuthenticationScheme, token);
					request.Headers.Add(ApiHeaders.OAuthProviderHeader, I.ToString());
					using var response = await httpClient.SendAsync(request, cancellationToken);
					Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
				}
		}
		async Task TestInvalidTransfers(IServerClient serverClient, CancellationToken cancellationToken)
		{
			var url = serverClient.Url;
			var token = serverClient.Token.Bearer;
			// check that 400s are returned appropriately
			using var httpClient = new HttpClient();

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.Transfer.Substring(1)))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(MediaTypeNames.Application.Json, response.Content.Headers.ContentType.MediaType);
				Assert.AreEqual(ErrorCode.ModelValidationFailure, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Put, url.ToString() + Routes.Transfer.Substring(1)))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync();
				var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
				Assert.AreEqual(MediaTypeNames.Application.Json, response.Content.Headers.ContentType.MediaType);
				Assert.AreEqual(ErrorCode.ModelValidationFailure, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.Transfer.Substring(1) + "?ticket=veryfaketicket"))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(MediaTypeNames.Application.Json, response.Content.Headers.ContentType.MediaType);
				Assert.AreEqual(HttpStatusCode.NotAcceptable, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.Transfer.Substring(1) + "?ticket=veryfaketicket"))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Octet));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(MediaTypeNames.Application.Json, response.Content.Headers.ContentType.MediaType);
				Assert.AreEqual(HttpStatusCode.Gone, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Put, url.ToString() + Routes.Transfer.Substring(1) + "?ticket=veryfaketicket"))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(MediaTypeNames.Application.Json, response.Content.Headers.ContentType.MediaType);
				Assert.AreEqual(HttpStatusCode.Gone, response.StatusCode);
			}
		}

		public Task Run(IServerClientFactory clientFactory, IServerClient serverClient, CancellationToken cancellationToken)
			=> Task.WhenAll(
				TestRequestValidation(serverClient, cancellationToken),
				TestOAuthFails(serverClient, cancellationToken),
				TestServerInformation(clientFactory, serverClient, cancellationToken),
				TestInvalidTransfers(serverClient, cancellationToken));
	}
}
