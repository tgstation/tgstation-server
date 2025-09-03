using System;
using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog.Context;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for <see cref="IApplicationBuilder"/>.
	/// </summary>
	static class ApplicationBuilderExtensions
	{
		/// <summary>
		/// If the server's swarm identifier should be pushed onto the log context for all requests.
		/// </summary>
		internal static bool LogSwarmIdentifier { get; set; }

		/// <summary>
		/// Return a <see cref="ConflictObjectResult"/> for <see cref="DbUpdateException"/>s.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		public static void UseDbConflictHandling(this IApplicationBuilder applicationBuilder)
		{
			ArgumentNullException.ThrowIfNull(applicationBuilder);

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
			ArgumentNullException.ThrowIfNull(applicationBuilder);
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
			ArgumentNullException.ThrowIfNull(applicationBuilder);

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
					});
				}
			});
		}

		/// <summary>
		/// Check that the API version is the current major version if it's present in the headers.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		public static void UseApiCompatibility(this IApplicationBuilder applicationBuilder)
		{
			ArgumentNullException.ThrowIfNull(applicationBuilder);

			applicationBuilder.Use(async (context, next) =>
			{
				var apiHeadersProvider = context.RequestServices.GetRequiredService<IApiHeadersProvider>();
				if (apiHeadersProvider.ApiHeaders?.Compatible(
					Version.Parse(
						MasterVersionsAttribute.Instance.RawGraphQLVersion)) == false)
				{
					await new BadRequestObjectResult(
						new ErrorMessageResponse(ErrorCode.ApiMismatch))
					.ExecuteResultAsync(new ActionContext
					{
						HttpContext = context,
					});
					return;
				}

				await next();
			});
		}

		/// <summary>
		/// Add the X-Powered-By response header.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		public static void UseServerBranding(this IApplicationBuilder applicationBuilder, IAssemblyInformationProvider assemblyInformationProvider)
		{
			ArgumentNullException.ThrowIfNull(applicationBuilder);
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);

			applicationBuilder.Use((context, next) =>
			{
				context.Response.Headers.Add("X-Powered-By", assemblyInformationProvider.VersionPrefix);
				return next();
			});
		}

		/// <summary>
		/// Add the X-Accel-Buffering response header.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		/// <remarks>This is used to avoid interruption to SignalR streams by Nginx.</remarks>
		public static void UseDisabledNginxProxyBuffering(this IApplicationBuilder applicationBuilder)
		{
			ArgumentNullException.ThrowIfNull(applicationBuilder);

			// https://www.nginx.com/resources/wiki/start/topics/examples/x-accel/#x-accel-buffering
			applicationBuilder.Use((context, next) =>
			{
				context.Response.Headers.Add("X-Accel-Buffering", "no");
				return next();
			});
		}

		/// <summary>
		/// Adds additional global <see cref="LogContext"/> to the request pipeline.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		/// <param name="swarmConfiguration">The <see cref="SwarmConfiguration"/>.</param>
		public static void UseAdditionalRequestLoggingContext(this IApplicationBuilder applicationBuilder, SwarmConfiguration swarmConfiguration)
		{
			ArgumentNullException.ThrowIfNull(applicationBuilder);
			ArgumentNullException.ThrowIfNull(swarmConfiguration);

			if (LogSwarmIdentifier && swarmConfiguration.Identifier != null)
				applicationBuilder.Use(async (context, next) =>
				{
					using (LogContext.PushProperty(SerilogContextHelper.SwarmIdentifierContextProperty, swarmConfiguration.Identifier))
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
