using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Base class for all API style controllers.
	/// </summary>
	[Produces(MediaTypeNames.Application.Json)]
	[ApiController]
	public abstract class ApiControllerBase : Controller
	{
		/// <inheritdoc />
		public sealed override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			// never cache an API response
			Response.Headers.Add(HeaderNames.CacheControl, new StringValues("no-cache"));

			var errorCase = await HookExecuteAction(
				() => base.OnActionExecutionAsync(context, next),
				Request.HttpContext.RequestAborted);

			if (errorCase != null)
				await errorCase.ExecuteResultAsync(context);
		}

		/// <summary>
		/// Hook for executing a request.
		/// </summary>
		/// <param name="executeAction">A <see cref="Func{TResult}"/> that should be invoked and its response awaited to continue normal execution of the request. Should NOT be called if this method returns a non-<see langword="null"/> value.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="IActionResult"/> that, if not <see langword="null"/>, is executed.</returns>
		protected virtual async ValueTask<IActionResult?> HookExecuteAction(Func<Task> executeAction, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(executeAction);

			await executeAction();
			return null;
		}
	}
}
