using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using Tgstation.Server.Common;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.Tests.Swarm
{
	sealed class SwarmRpcMapper : IRequestSwarmRegistrationParser
	{
		List<(SwarmConfiguration, TestableSwarmNode)> configToControllers;
		Guid? incomingRegistrationId;

		public SwarmRpcMapper(Mock<IHttpClient> clientMock)
		{
			clientMock
				.Setup(x => x.SendAsync(It.IsNotNull<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
				.Returns(MapRequest);
		}

		public Guid GetRequestRegistrationId(HttpRequest request)
		{
			Assert.IsTrue(incomingRegistrationId.HasValue);
			var result = incomingRegistrationId.Value;
			incomingRegistrationId = null;
			return result;
		}

		public void Register(List<(SwarmConfiguration, TestableSwarmNode)> configToControllers)
		{
			this.configToControllers = configToControllers;
		}

		async Task<HttpResponseMessage> MapRequest(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			var (config, node) = configToControllers.FirstOrDefault(
				pair => pair.Item1.Address.IsBaseOf(request.RequestUri));

			if (config == default)
				Assert.Fail($"Invalid node address: {request.RequestUri}");

			if (!node.Initialized)
			{
				throw new HttpRequestException("Can't connect to uninitialized node!");
			}

			var controller = node.Controller;

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

			var controllerMethod = controller
				.GetType()
				.GetMethods()
				.Select(method => (method, (HttpMethodAttribute)method.GetCustomAttribute(targetAttribute)))
				.Where(pair => pair.Item2 != null
					&& pair.Item2.HttpMethods.Count() == 1
					&& pair.Item2.HttpMethods.All(supportedMethod => supportedMethod.Equals(request.Method.Method))
					&& pair.Item2.Template == route)
				.Select(pair => pair.method)
				.SingleOrDefault();

			if (controllerMethod == default)
				Assert.Fail($"SwarmController has no method with attribute {targetAttribute}!");

			// We're not testing OnActionExecutingAsync, that's covered by integration.
			if (request.Headers.TryGetValues(SwarmConstants.RegistrationIdHeader, out var values) && values.Count() == 1)
				node.RpcMapper.incomingRegistrationId = Guid.Parse(values.First());

			var args = new List<object>();
			if (isDataRequest)
			{
				var dataType = controllerMethod.GetParameters().First().ParameterType;
				var json = await request.Content.ReadAsStringAsync(cancellationToken);
				var parameter = JsonConvert.DeserializeObject(json, dataType, SwarmService.SerializerSettings);
				args.Add(parameter);
			}

			IActionResult result;

			var response = new HttpResponseMessage();
			try
			{
				if (controllerMethod.ReturnType != typeof(IActionResult))
				{
					Assert.AreEqual(typeof(Task<IActionResult>), controllerMethod.ReturnType);
					args.Add(cancellationToken);
					var invocationTask = (Task<IActionResult>)controllerMethod.Invoke(controller, args.ToArray());
					result = await invocationTask;
				}
				else
				{
					result = (IActionResult)controllerMethod.Invoke(controller, args.ToArray());

					// simulate worst case, request completed but was aborted before server replied
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
			catch
			{
				response.Dispose();
				throw;
			}

			// manually checked all controller response types
			// Fobid, NoContent, Conflict, StatusCode
			if (result is ForbidResult forbidResult)
				response.StatusCode = HttpStatusCode.Forbidden;
			else if (result is NoContentResult noContentResult)
				response.StatusCode = (HttpStatusCode)noContentResult.StatusCode;
			else if (result is ConflictResult conflictResult)
				response.StatusCode = (HttpStatusCode)conflictResult.StatusCode;
			else if (result is ObjectResult objectResult)
				response.StatusCode = (HttpStatusCode)objectResult.StatusCode;
			else if (result is StatusCodeResult statusCodeResult)
				response.StatusCode = (HttpStatusCode)statusCodeResult.StatusCode;
			else
			{
				response.Dispose();
				Assert.Fail($"Unrecognized result type: {result.GetType()}");
			}

			return response;
		}
	}
}
