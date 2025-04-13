using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.Transfer;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Implementation of the <see cref="TransferController"/> for the swarm service.
	/// </summary>
	public sealed class SwarmTransferController : ControllerBase
	{
		/// <summary>
		/// The <see cref="IFileTransferStreamHandler"/> for the <see cref="SwarmTransferController"/>.
		/// </summary>
		readonly IFileTransferStreamHandler fileTransferService;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmTransferController"/> class.
		/// </summary>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		public SwarmTransferController(
			IFileTransferStreamHandler fileTransferService)
		{
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
		}

		/// <summary>
		/// Downloads a file with a given <paramref name="ticket"/>.
		/// </summary>
		/// <param name="ticket">The <see cref="FileTicketResponse.FileTicket"/> for the download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the method.</returns>
		[Authorize(Policy = SwarmConstants.AuthenticationSchemeAndPolicy)]
		public ValueTask<IActionResult> Download([Required, FromQuery] string ticket, CancellationToken cancellationToken)
			=> fileTransferService.GenerateDownloadResponse(this, ticket, cancellationToken);
	}
}
