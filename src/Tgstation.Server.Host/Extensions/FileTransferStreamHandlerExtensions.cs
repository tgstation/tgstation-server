using System;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Transfer;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IFileTransferStreamHandler"/>.
	/// </summary>
	static class FileTransferStreamHandlerExtensions
	{
		/// <summary>
		/// Downloads a file with a given <paramref name="ticket"/>.
		/// </summary>
		/// <param name="fileTransferService">The <see cref="IFileTransferStreamHandler"/>.</param>
		/// <param name="controller">The <see cref="ControllerBase"/> the request is coming from.</param>
		/// <param name="ticket">The <see cref="FileTicketResponse.FileTicket"/> for the download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the method.</returns>
		public static async ValueTask<IActionResult> GenerateDownloadResponse(
			this IFileTransferStreamHandler fileTransferService,
			ControllerBase controller,
			string ticket,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(fileTransferService);

			ArgumentNullException.ThrowIfNull(controller);

			if (ticket == null)
				return controller.BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			var streamAccept = new MediaTypeHeaderValue(MediaTypeNames.Application.Octet);
			if (!controller.Request.GetTypedHeaders().Accept.Any(streamAccept.IsSubsetOf))
				return controller.StatusCode((int)HttpStatusCode.NotAcceptable, new ErrorMessageResponse(ErrorCode.BadHeaders)
				{
					AdditionalData = $"File downloads must accept both {MediaTypeNames.Application.Octet} and {MediaTypeNames.Application.Json}!",
				});

			var fileTicketResult = new FileTicketResponse
			{
				FileTicket = ticket,
			};

			var (stream, errorMessage) = await fileTransferService.RetrieveDownloadStream(fileTicketResult, cancellationToken);
			try
			{
				if (errorMessage != null)
					return controller.Conflict(errorMessage);

				if (stream == null)
					return controller.Gone();

				return new LimitedStreamResult(stream);
			}
			catch
			{
				if (stream != null)
					await stream.DisposeAsync();

				throw;
			}
		}
	}
}
