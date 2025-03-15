using System;

using Grpc.Core;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Swarm.Grpc
{
	/// <summary>
	/// gRPC representation of a <see cref="FileTicketResponse"/>.
	/// </summary>
	public sealed partial class DownloadTicket
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DownloadTicket"/> class.
		/// </summary>
		/// <param name="fileTicketResponse">The <see cref="FileTicketResponse"/> to build from.</param>
		public DownloadTicket(FileTicketResponse fileTicketResponse)
			: this()
		{
			ArgumentNullException.ThrowIfNull(fileTicketResponse);
			FileTicket = fileTicketResponse.FileTicket;
		}

		/// <summary>
		/// Convert the <see cref="DownloadTicket"/> to a <see cref="FileTicketResponse"/>.
		/// </summary>
		/// <returns>The converted <see cref="FileTicketResponse"/>.</returns>
		public FileTicketResponse ToFileTicketResponse()
		{
			try
			{
				ArgumentException.ThrowIfNullOrWhiteSpace(FileTicket);
				return new FileTicketResponse
				{
					FileTicket = FileTicket,
				};
			}
			catch
			{
				throw new RpcException(
					new Status(StatusCode.InvalidArgument, "Invalid file ticket!"));
			}
		}
	}
}
