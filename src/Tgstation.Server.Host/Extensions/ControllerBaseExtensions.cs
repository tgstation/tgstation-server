using System;
using System.Net;
using System.Net.Mime;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

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
		/// Generic 201 response with a given <paramref name="payload"/>.
		/// </summary>
		/// <param name="controller">The <see cref="ControllerBase"/> the request is coming from.</param>
		/// <param name="payload">The accompanying API payload.</param>
		/// <returns>A <see cref="HttpStatusCode.Created"/> <see cref="ObjectResult"/> with the given <paramref name="payload"/>.</returns>
		public static ObjectResult Created(this ControllerBase controller, object payload) => controller.StatusCode(HttpStatusCode.Created, payload);

		/// <summary>
		/// Generic 401 response.
		/// </summary>
		/// <param name="controller">The <see cref="ControllerBase"/> the request is coming from.</param>
		/// <returns>An <see cref="ObjectResult"/> with <see cref="HttpStatusCode.NotFound"/>.</returns>
		public static ObjectResult Unauthorized(this ControllerBase controller) => controller.StatusCode(HttpStatusCode.Unauthorized, null);

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

		/// <summary>
		/// Try to serve a given file <paramref name="path"/>.
		/// </summary>
		/// <param name="controller">The <see cref="ControllerBase"/>.</param>
		/// <param name="hostEnvironment">The <see cref="IWebHostEnvironment"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/>.</param>
		/// <param name="path">The path to the file in the 'wwwroot'.</param>
		/// <returns>A <see cref="VirtualFileResult"/> if the file was found. <see langword="null"/> otherwise.</returns>
		public static VirtualFileResult? TryServeFile(this ControllerBase controller, IWebHostEnvironment hostEnvironment, ILogger logger, string path)
		{
			ArgumentNullException.ThrowIfNull(controller);
			ArgumentNullException.ThrowIfNull(hostEnvironment);
			ArgumentNullException.ThrowIfNull(logger);
			ArgumentNullException.ThrowIfNull(path);

			var fileInfo = hostEnvironment.WebRootFileProvider.GetFileInfo(path);
			if (fileInfo.Exists)
			{
				logger.LogTrace("Serving static file \"{filename}\"...", path);
				var contentTypeProvider = new FileExtensionContentTypeProvider();
				if (!contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType))
					contentType = MediaTypeNames.Application.Octet;
				else if (contentType == MediaTypeNames.Application.Json)
					controller.Response.Headers.Add(
						HeaderNames.CacheControl,
						new StringValues(new[] { "public", "max-age=31536000", "immutable" }));

				return controller.File(path, contentType);
			}

			return null;
		}
	}
}
