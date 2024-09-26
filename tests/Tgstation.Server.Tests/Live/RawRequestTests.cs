using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Extensions;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host;
using Tgstation.Server.Client.GraphQL;
using System.Linq;
using StrawberryShake;

namespace Tgstation.Server.Tests.Live
{
	static class RawRequestTests
	{
		static async Task TestRequestValidation(IRestServerClient serverClient, CancellationToken cancellationToken)
		{
			var url = serverClient.Url;
			var token = serverClient.Token.Bearer;
			// check that 400s are returned appropriately
			using var httpClient = new HttpClient();
			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.NotAcceptable, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Xml));
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.NotAcceptable, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.BadHeaders, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ServerInformationResponse>(content);
				Assert.AreEqual(ApiHeaders.Version, message.ApiVersion);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/6.0.0");
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.ApiMismatch, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(url.ToString(), Routes.Administration.AsSpan(1))))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/6.0.0");
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.ApiMismatch, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, string.Concat(url.ToString(), Routes.Administration.AsSpan(1))))
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
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.ModelValidationFailure, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, string.Concat(url.ToString(), Routes.DreamDaemon.AsSpan(1))))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, string.Concat(url.ToString(), Routes.DreamDaemon.AsSpan(1))))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.InstanceHeaderRequired, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme.ToLower(), token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.BadHeaders, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.OAuthAuthenticationScheme, token);
				request.Headers.Add(ApiHeaders.OAuthProviderHeader, "FakeProvider");
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.BadHeaders, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.ApiRoot.TrimStart('/')))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(
					ApiHeaders.BasicAuthenticationScheme,
					Convert.ToBase64String(
						Encoding.UTF8.GetBytes(":")));
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(Api.Models.ErrorCode.BadHeaders, message.ErrorCode);
			}
		}

		static async Task TestServerInformation(IRestServerClientFactory clientFactory, IRestServerClient serverClient, CancellationToken cancellationToken)
		{
			var serverInfo = await serverClient.ServerInformation(default);

			Assert.AreEqual(ApiHeaders.Version, serverInfo.ApiVersion);
			var assemblyVersion = typeof(IServer).Assembly.GetName().Version.Semver();
			Assert.AreEqual(assemblyVersion, serverInfo.Version);
			Assert.AreEqual(10U, serverInfo.MinimumPasswordLength);
			Assert.AreEqual(11U, serverInfo.InstanceLimit);
			Assert.AreEqual(150U, serverInfo.UserLimit);
			Assert.AreEqual(47U, serverInfo.UserGroupLimit);
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), serverInfo.WindowsHost);

			//check that modifying the token even slightly fucks up the auth
			var newToken = new TokenResponse
			{
				Bearer = serverClient.Token.Bearer + '0'
			};

			var badClient = clientFactory.CreateFromToken(serverClient.Url, newToken);
			await ApiAssert.ThrowsException<UnauthorizedException, AdministrationResponse>(() => badClient.Administration.Read(false, cancellationToken));
			await ApiAssert.ThrowsException<UnauthorizedException, ServerInformationResponse>(() => badClient.ServerInformation(cancellationToken));
		}

		static async Task TestOAuthFails(IRestServerClient serverClient, CancellationToken cancellationToken)
		{
			var url = serverClient.Url;
			var token = serverClient.Token.Bearer;
			// check that 400s are returned appropriately
			using var httpClient = new HttpClient();

			// just hitting each type of oauth provider for coverage
			foreach (var I in Enum.GetValues(typeof(Api.Models.OAuthProvider)))
				using (var request = new HttpRequestMessage(HttpMethod.Post, url.ToString() + Routes.ApiRoot.TrimStart('/')))
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

		static async Task TestInvalidTransfers(IRestServerClient serverClient, CancellationToken cancellationToken)
		{
			var url = serverClient.Url;
			var token = serverClient.Token.Bearer;
			// check that 400s are returned appropriately
			using var httpClient = new HttpClient();

			using (var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(url.ToString(), Routes.Transfer.AsSpan(1))))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(MediaTypeNames.Application.Json, response.Content.Headers.ContentType.MediaType);
				Assert.AreEqual(Api.Models.ErrorCode.ModelValidationFailure, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Put, string.Concat(url.ToString(), Routes.Transfer.AsSpan(1))))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
				Assert.AreEqual(MediaTypeNames.Application.Json, response.Content.Headers.ContentType.MediaType);
				Assert.AreEqual(Api.Models.ErrorCode.ModelValidationFailure, message.ErrorCode);
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(url.ToString(), Routes.Transfer.AsSpan(1), "?ticket=veryfaketicket")))
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

			using (var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(url.ToString(), Routes.Transfer.AsSpan(1), "?ticket=veryfaketicket")))
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

			using (var request = new HttpRequestMessage(HttpMethod.Put, string.Concat(url.ToString(), Routes.Transfer.AsSpan(1), "?ticket=veryfaketicket")))
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

		static async Task RegressionTestForLeakedPasswordHashesBug(IRestServerClient serverClient, CancellationToken cancellationToken)
		{
			// See what https://github.com/tgstation/tgstation-server/commit/6c8dc87c4af36620885b262175d7974aca2b3c2b fixed

			var url = serverClient.Url;
			var token = serverClient.Token.Bearer;
			// check that 400s are returned appropriately
			using var httpClient = new HttpClient();

			using (var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(url.ToString(), Routes.User.AsSpan(1))))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				Assert.IsFalse(content.Contains("passwordHash"));
			}

			using (var request = new HttpRequestMessage(HttpMethod.Get, url.ToString() + Routes.User[1..] + '/' + Routes.List + "?pageSize=100"))
			{
				request.Headers.Accept.Clear();
				request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
				request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.BearerAuthenticationScheme, token);
				using var response = await httpClient.SendAsync(request, cancellationToken);
				Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
				var content = await response.Content.ReadAsStringAsync(cancellationToken);
				Assert.IsFalse(content.Contains("passwordHash"));
			}
		}

		class FuncProxiedJobsHub : IJobsHub
		{
			public Func<JobResponse, CancellationToken, Task> ProxyFunc { get; set; }

			public Task ReceiveJobUpdate(JobResponse job, CancellationToken cancellationToken)
				=> ProxyFunc(job, cancellationToken);
		}

		static async Task TestSignalRUsage(IRestServerClientFactory serverClientFactory, IRestServerClient serverClient, CancellationToken cancellationToken)
		{
			// test regular creation works without error
			var hubConnectionBuilder = new HubConnectionBuilder();

			var tokenRetrivalFunc = () => Task.FromResult("FakeToken");

			hubConnectionBuilder.WithUrl(
				new Uri(serverClient.Url, Routes.JobsHub),
				HttpTransportType.ServerSentEvents,
				options =>
				{
					options.AccessTokenProvider = () => tokenRetrivalFunc();
					((IApiClient)typeof(RestServerClient)
						.GetField(
							"apiClient",
							BindingFlags.NonPublic | BindingFlags.Instance)
						.GetValue(serverClient))
						.Headers
						.SetHubConnectionHeaders(options.Headers);
				})
				.AddNewtonsoftJsonProtocol(options =>
				{
					// we can get away without setting the serializer settings here
				});

			hubConnectionBuilder.ConfigureLogging(
				loggingBuilder =>
				{
					loggingBuilder.SetMinimumLevel(LogLevel.Trace);
					loggingBuilder.AddConsole();
					loggingBuilder
						.Services
						.TryAddEnumerable(
							ServiceDescriptor.Singleton<ILoggerProvider, HardFailLoggerProvider>());
				});

			var proxy = new FuncProxiedJobsHub();
			HubConnection hubConnection;
			HardFailLoggerProvider.BlockFails = true;
			try
			{
				await using (hubConnection = hubConnectionBuilder.Build())
				{
					Assert.AreEqual(HubConnectionState.Disconnected, hubConnection.State);
					hubConnection.ProxyOn<IJobsHub>(proxy);

					var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => hubConnection.StartAsync(cancellationToken));

					Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
					Assert.AreEqual(HubConnectionState.Disconnected, hubConnection.State);

					tokenRetrivalFunc = () => Task.FromResult(serverClient.Token.Bearer);
					await hubConnection.StartAsync(cancellationToken);

					Assert.AreEqual(HubConnectionState.Connected, hubConnection.State);
				}

				Assert.AreEqual(HubConnectionState.Disconnected, hubConnection.State);

				var createRequest = new UserCreateRequest
				{
					Enabled = true,
					Name = "SignalRTestUser",
					Password = "asdfasdfasdfasdfasdf"
				};

				var testUser = await serverClient.Users.Create(createRequest, cancellationToken);
				await using var testUserClient = await serverClientFactory.CreateFromLogin(serverClient.Url, createRequest.Name, createRequest.Password, cancellationToken: cancellationToken);
				await using var testUserConn1 = (HubConnection)await testUserClient.SubscribeToJobUpdates(proxy, cancellationToken: cancellationToken);

				await serverClient.Users.Update(new UserUpdateRequest
				{
					Id = testUser.Id,
					Enabled = false,
				}, cancellationToken);

				// need a second here
				for (var i = 0; i < 10 && testUserConn1.State == HubConnectionState.Connected; ++i)
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

				Assert.AreNotEqual(HubConnectionState.Connected, testUserConn1.State);

				await ApiAssert.ThrowsException<InsufficientPermissionsException>(async () => await testUserClient.SubscribeToJobUpdates(proxy, cancellationToken: cancellationToken));
			}
			finally
			{
				HardFailLoggerProvider.BlockFails = false;
			}
		}

		static async Task TestGraphQLLogin(IRestServerClientFactory clientFactory, IRestServerClient restClient, CancellationToken cancellationToken)
		{
			await using var gqlClient = new GraphQLServerClientFactory(clientFactory).CreateUnauthenticated(restClient.Url);
			var result = await gqlClient.RunOperation(client => client.Login.ExecuteAsync(cancellationToken), cancellationToken);

			Assert.IsNotNull(result.Data);
			Assert.IsNull(result.Data.Login.Bearer);
			Assert.IsNotNull(result.Data.Login.Errors);
			Assert.AreEqual(1, result.Data.Login.Errors.Count);
			var castResult = result.Data.Login.Errors[0] is ILogin_Login_Errors_ErrorMessageError loginError;
			Assert.IsTrue(castResult);
			loginError = (ILogin_Login_Errors_ErrorMessageError)result.Data.Login.Errors[0];
			Assert.AreEqual(Client.GraphQL.ErrorCode.BadHeaders, loginError.ErrorCode.Value);
			Assert.IsNotNull(loginError.Message);
			Assert.IsNotNull(loginError.AdditionalData);
		}

		public static Task Run(IRestServerClientFactory clientFactory, IRestServerClient serverClient, CancellationToken cancellationToken)
			=> Task.WhenAll(
				TestRequestValidation(serverClient, cancellationToken),
				TestGraphQLLogin(clientFactory, serverClient, cancellationToken),
				TestOAuthFails(serverClient, cancellationToken),
				TestServerInformation(clientFactory, serverClient, cancellationToken),
				TestInvalidTransfers(serverClient, cancellationToken),
				RegressionTestForLeakedPasswordHashesBug(serverClient, cancellationToken),
				TestSignalRUsage(clientFactory, serverClient, cancellationToken));
	}
}
