using System;
using System.Net;

using Microsoft.AspNetCore.Mvc;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="ControllerBase"/>.
	/// </summary>
	static class ControllerBaseExtensions
	{
		/// <summary>
		/// Generic 410 response.
		/// </summary>
		/// <param name="controller">The <see cref="ControllerBase"/> the request is coming from.</param>
		/// <returns>An <see cref="ObjectResult"/> with <see cref="HttpStatusCode.Gone"/>.</returns>
		public static ObjectResult Gone(this ControllerBase controller)
			=> controller?.StatusCode(HttpStatusCode.Gone, new ErrorMessageResponse(ErrorCode.ResourceNotPresent)) ?? throw new ArgumentNullException(nameof(controller));

		/// <summary>
		/// Strongly type calls to <see cref="ControllerBase.StatusCode(int, object)"/>.
		/// </summary>
		/// <param name="controller">The <see cref="ControllerBase"/> the request is coming from.</param>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
		/// <param name="errorMessage">The accompanying <see cref="ErrorMessageResponse"/> payload.</param>
		/// <returns>A <see cref="StatusCodeResult"/> with the given <paramref name="statusCode"/>.</returns>
		public static ObjectResult StatusCode(this ControllerBase controller, HttpStatusCode statusCode, object? errorMessage)
			=> controller?.StatusCode((int)statusCode, errorMessage) ?? throw new ArgumentNullException(nameof(controller));
	}
}
