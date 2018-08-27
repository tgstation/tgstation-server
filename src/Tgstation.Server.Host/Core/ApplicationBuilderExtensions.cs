using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Extensions for <see cref="IApplicationBuilder"/>
	/// </summary>
	static class ApplicationBuilderExtensions
	{
		/// <summary>
		/// Return a <see cref="ConflictObjectResult"/> for <see cref="DbUpdateException"/>s
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		public static void UseDbConflictHandling(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			applicationBuilder.Use(async (context, next) =>
			{
				try
				{
					await next().ConfigureAwait(false);
				}
				catch (DbUpdateException e)
				{
					await new ConflictObjectResult(new ErrorMessage { Message = String.Format(CultureInfo.InvariantCulture, "A database conflict has occurred: {0}", (e.InnerException ?? e).Message) }).ExecuteResultAsync(new ActionContext
					{
						HttpContext = context
					}).ConfigureAwait(false);
				}
			});
		}

		/// <summary>
		/// Suppress TaskCanceledException warnings when a user aborts a request
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		public static void UseCancelledRequestSuppression(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			applicationBuilder.Use(async (context, next) =>
			{
				try
				{
					await next().ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }
			});
		}
	}
}
