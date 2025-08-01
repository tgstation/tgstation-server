using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using Tgstation.Server.Common.Tests;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Transfer;

namespace Tgstation.Server.Host.Swarm.Tests
{
	sealed class SwarmRpcMapper : IDisposable
	{
		public bool AsyncRequests { get; set; }

		readonly ILogger logger;

		readonly Func<SwarmService, FileTransferService, SwarmController> createSwarmController;

		List<(SwarmConfiguration, FileTransferService, TestableSwarmNode)> configToNodes;

		int serverErrorCount;

		public SwarmRpcMapper(Func<SwarmService, FileTransferService, SwarmController> createSwarmController, ILogger logger, out HttpMessageHandler handlerMock)
		{
			this.createSwarmController = createSwarmController;
			handlerMock = new MockHttpMessageHandler(MapRequest);
			this.logger = logger;
			AsyncRequests = true;
		}

		public void Dispose()
		{
			Assert.AreEqual(0, serverErrorCount);
		}

		public void Register(List<(SwarmConfiguration, FileTransferService, TestableSwarmNode)> configToNodes)
		{
			this.configToNodes = configToNodes;
		}

		async Task<HttpResponseMessage> MapRequest(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			var (config, transferService, node) = configToNodes.FirstOrDefault(
				pair => pair.Item1.Address.IsBaseOf(request.RequestUri));

			if (config == default)
				Assert.Fail($"Invalid node address: {request.RequestUri}");

			if (!node.WebServerOpen)
			{
				throw new HttpRequestException("Can't connect to uninitialized node!");
			}

			if (node.Shutdown)
			{
				throw new HttpRequestException("Can't connect to shutdown node!");
			}

			var controller = createSwarmController(node.Service, transferService);

			Type targetAttribute = null;
			bool isDataRequest = false;
			switch (request.Method.Method.ToUpperInvariant())
			{
				case "GET":
					targetAttribute = typeof(HttpGetAttribute);
					break;
				case "POST":
					targetAttribute = typeof(HttpPostAttribute);
					isDataRequest = true;
					break;
				case "PUT":
					targetAttribute = typeof(HttpPutAttribute);
					isDataRequest = true;
					break;
				case "DELETE":
					targetAttribute = typeof(HttpDeleteAttribute);
					break;
				case "PATCH":
					targetAttribute = typeof(HttpPatchAttribute);
					isDataRequest = true;
					break;
				default:
					Assert.Fail($"Unknown request method: {request.Method.Method}");
					break;
			}

			var stringUrl = request.RequestUri.ToString();
			var rootIndex = stringUrl.IndexOf(SwarmConstants.ControllerRoute);
			if (rootIndex == -1)
				Assert.Fail($"Invalid Swarm route: {stringUrl}");

			var route = stringUrl[(rootIndex + SwarmConstants.ControllerRoute.Length)..].TrimStart('/');
			var queryIndex = route.IndexOf('?');

			string ticketArg = null;
			if(queryIndex != -1)
			{
				var ticketEncoded = route[(queryIndex + "?ticket=".Length)..];
				ticketArg = HttpUtility.UrlDecode(ticketEncoded);
				route = route[..queryIndex];
			}

			var controllerMethod = controller
				.GetType()
				.GetMethods()
				.Select(method => (method, (HttpMethodAttribute)method.GetCustomAttribute(targetAttribute)))
				.Where(pair =>
				{
					return pair.Item2 != null
						&& pair.Item2.HttpMethods.Count() == 1
						&& pair.Item2.HttpMethods.All(supportedMethod => supportedMethod.Equals(request.Method.Method))
						&& (pair.Item2.Template ?? String.Empty) == route;
				})
				.Select(pair => pair.method)
				.SingleOrDefault();

			if (controllerMethod == default)
				Assert.Fail($"SwarmController has no method with attribute {targetAttribute}!");

			IActionResult result;
			var hasRegistrationHeader = request.Headers.TryGetValues(SwarmConstants.RegistrationIdHeader, out var values)
				&& values.Count() == 1;
			var response = new HttpResponseMessage();
			try
			{
				// We're not testing OnActionExecutingAsync, that's covered by integration.
				logger.LogTrace("RPC Calling {method}", controllerMethod);
				if (hasRegistrationHeader)
				{
					var mockRequest = new Mock<HttpRequest>();

					var headers = new HeaderDictionary();
					foreach (var header in request.Headers)
						headers.Add(new KeyValuePair<string, StringValues>(header.Key, new StringValues(header.Value.ToArray())));

					mockRequest.SetupGet(x => x.Headers).Returns(headers);
					var mockHttpContext = new Mock<HttpContext>();
					mockHttpContext.SetupGet(x => x.Request).Returns(mockRequest.Object);

					controller
						.ControllerContext
						.HttpContext = mockHttpContext.Object;

					var args = new List<object>();
					if (isDataRequest && request.Content != null)
					{
						var dataType = controllerMethod.GetParameters().First().ParameterType;
						var json = await request.Content.ReadAsStringAsync(cancellationToken);
						var parameter = JsonConvert.DeserializeObject(json, dataType, SwarmConstants.SerializerSettings);
						args.Add(parameter);
					}

					if (ticketArg != null)
						args.Add(ticketArg);

					if (AsyncRequests)
						await Task.Yield();

					if (controllerMethod.ReturnType != typeof(IActionResult))
					{
						var isValueTask = controllerMethod.ReturnType == typeof(ValueTask<IActionResult>);
						if (!isValueTask)
							Assert.AreEqual(typeof(Task<IActionResult>), controllerMethod.ReturnType);

						var lastParam = controllerMethod.GetParameters().LastOrDefault();
						if (lastParam?.ParameterType == typeof(CancellationToken))
							args.Add(cancellationToken);

						var rawTask = controllerMethod.Invoke(controller, args.ToArray());
						Task<IActionResult> invocationTask;
						if (isValueTask)
							invocationTask = ((ValueTask<IActionResult>)rawTask).AsTask();
						else
							invocationTask = (Task<IActionResult>)rawTask;

						result = await invocationTask;
					}
					else
					{
						result = (IActionResult)controllerMethod.Invoke(controller, args.ToArray());
					}

					// simulate worst case, request completed but was aborted before server replied
					cancellationToken.ThrowIfCancellationRequested();
				}
				else
				{
					result = controller.BadRequest();
				}

				// manually checked all controller response types
				// Fobid, NoContent, Conflict, StatusCode
				if (result is ForbidResult forbidResult)
					response.StatusCode = HttpStatusCode.Forbidden;
				else if (result is JsonResult jsonResult)
				{
					response.StatusCode = HttpStatusCode.OK;
					response.Content = new StringContent(JsonConvert.SerializeObject(jsonResult.Value));
				}
				else if (result is IStatusCodeActionResult statusCodeResult)
					response.StatusCode = (HttpStatusCode)statusCodeResult.StatusCode;
				else if (result is LimitedStreamResult streamResult)
					await using (streamResult)
					{
						response.StatusCode = HttpStatusCode.OK;
						var buffer = new MemoryStream();
						var stream = await streamResult.GetResult(cancellationToken);
						await stream.CopyToAsync(buffer, cancellationToken);
						response.Content = new StreamContent(buffer);
					}
				else
				{
					response.Dispose();
					Assert.Fail($"Unrecognized result type: {result.GetType()}");
				}

				return response;
			}
			catch (Exception ex)
			{
				if (ex is not OperationCanceledException)
				{
					logger.LogCritical(ex, "Error in request to {nodeId}!", config.Identifier);
					++serverErrorCount;
				}

				response.Dispose();
				throw;
			}
		}
	}
}
