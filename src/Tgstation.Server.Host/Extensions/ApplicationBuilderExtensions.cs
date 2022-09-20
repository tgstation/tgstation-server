using System;
using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for <see cref="IApplicationBuilder"/>.
	/// </summary>
	static class ApplicationBuilderExtensions
	{
		/// <summary>
		/// Return a <see cref="ConflictObjectResult"/> for <see cref="DbUpdateException"/>s.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		public static void UseDbConflictHandling(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));

			applicationBuilder.Use(async (context, next) =>
			{
				var logger = GetLogger(context);
				try
				{
					await next();
				}
				catch (DbUpdateException e)
				{
					if (e.InnerException is OperationCanceledException)
					{
						logger.LogTrace(e, "Rethrowing DbUpdateException as OperationCanceledException");
						throw e.InnerException;
					}

					logger.LogDebug(e, "Database conflict!");
					await new ConflictObjectResult(new ErrorMessageResponse(ErrorCode.DatabaseIntegrityConflict)
					{
						AdditionalData = String.Format(CultureInfo.InvariantCulture, (e.InnerException ?? e).Message),
					}).ExecuteResultAsync(new ActionContext
					{
						HttpContext = context,
					});
				}
			});
		}

		/// <summary>
		/// Suppress <see cref="global::System.Threading.Tasks.TaskCanceledException"/> warnings when a user aborts a request.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		public static void UseCancelledRequestSuppression(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			applicationBuilder.Use(async (context, next) =>
			{
				var logger = GetLogger(context);
				try
				{
					await next();
				}
				catch (OperationCanceledException ex)
				{
					logger.LogDebug(ex, "Request cancelled!");
				}
			});
		}

		/// <summary>
		/// Suppress all in flight <see cref="Exception"/>s for the request with error 500.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		public static void UseServerErrorHandling(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			applicationBuilder.Use(async (context, next) =>
			{
				var logger = GetLogger(context);
				try
				{
					await next();
				}
				catch (Exception e)
				{
					logger.LogError(e, "Failed request!");
					await new JsonResult(
						new ErrorMessageResponse(ErrorCode.InternalServerError)
						{
							AdditionalData = e.ToString(),
						})
					{
						StatusCode = (int)HttpStatusCode.InternalServerError,
					}
					.ExecuteResultAsync(new ActionContext
					{
						HttpContext = context,
					})
					;
				}
			});
		}

		/// <summary>
		/// Add the X-Powered-By response header.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		public static void UseServerBranding(this IApplicationBuilder applicationBuilder, IAssemblyInformationProvider assemblyInformationProvider)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			if (assemblyInformationProvider == null)
				throw new ArgumentNullException(nameof(assemblyInformationProvider));

			applicationBuilder.Use(async (context, next) =>
			{
				context.Response.Headers.Add("X-Powered-By", assemblyInformationProvider.VersionPrefix);
				await next();
			});
		}

		/// <summary>
		/// Gets a <see cref="ILogger"/> from a given <paramref name="httpContext"/>.
		/// </summary>
		/// <param name="httpContext">The <see cref="HttpContext"/> to get the <see cref="ILogger"/> from.</param>
		/// <returns>A new <see cref="ILogger"/>.</returns>
		static ILogger GetLogger(HttpContext httpContext) => httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ApplicationBuilderExtensions));
	}
}
