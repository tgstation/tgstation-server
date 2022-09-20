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
	/// <see cref="IActionResultExecutor{TResult}"/> for <see cref="LimitedFileStreamResult"/>s.
	/// </summary>
	public class LimitedFileStreamResultExecutor : FileResultExecutorBase, IActionResultExecutor<LimitedFileStreamResult>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LimitedFileStreamResultExecutor"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="FileResultExecutorBase"/>.</param>
		public LimitedFileStreamResultExecutor(ILogger<LimitedFileStreamResultExecutor> logger)
			: base(logger)
		{
		}

		/// <inheritdoc />
		public async Task ExecuteAsync(ActionContext context, LimitedFileStreamResult result)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			if (result == null)
				throw new ArgumentNullException(nameof(result));

			using (result.FileStream)
			{
				var contentLength = result.FileStream.Length;
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
							result.FileStream,
							outputStream,
							contentLength,
							BufferSize,
							cancellationToken)
							;
					}
					else
					{
						result.FileStream.Seek(range.From.Value, SeekOrigin.Begin);
						await StreamCopyOperation.CopyToAsync(
							result.FileStream,
							outputStream,
							rangeLength,
							BufferSize,
							cancellationToken)
							;
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
