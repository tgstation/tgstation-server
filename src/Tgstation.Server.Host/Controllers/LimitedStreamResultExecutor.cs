using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="IActionResultExecutor{TResult}"/> for <see cref="LimitedStreamResult"/>s.
	/// </summary>
	public class LimitedStreamResultExecutor : FileResultExecutorBase, IActionResultExecutor<LimitedStreamResult>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LimitedStreamResultExecutor"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="FileResultExecutorBase"/>.</param>
		public LimitedStreamResultExecutor(ILogger<LimitedStreamResultExecutor> logger)
			: base(logger)
		{
		}

		/// <inheritdoc />
		public async Task ExecuteAsync(ActionContext context, LimitedStreamResult result)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			if (result == null)
				throw new ArgumentNullException(nameof(result));

			await using (result.Stream)
			{
				var contentLength = result.Stream.Length;
				var (range, rangeLength, serveBody) = SetHeadersAndLog(context, result, contentLength, result.EnableRangeProcessing);
				if (!serveBody)
					return;

				try
				{
					var cancellationToken = context.HttpContext.RequestAborted;
					var outputStream = context.HttpContext.Response.Body;
					if (range == null)
					{
						await StreamCopyOperation.CopyToAsync(
							result.Stream,
							outputStream,
							contentLength,
							BufferSize,
							cancellationToken);
					}
					else
					{
						result.Stream.Seek(range.From.Value, SeekOrigin.Begin);
						await StreamCopyOperation.CopyToAsync(
							result.Stream,
							outputStream,
							rangeLength,
							BufferSize,
							cancellationToken);
					}
				}
				catch (OperationCanceledException)
				{
					// Don't throw this exception, it's most likely caused by the client disconnecting.
					// However, if it was cancelled for any other reason we need to prevent empty responses.
					context.HttpContext.Abort();
				}
			}
		}
	}
}
