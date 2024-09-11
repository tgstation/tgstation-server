using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For transferring data <see cref="Stream"/>s.
	/// </summary>
	public interface ITransferClient
	{
		/// <summary>
		/// Downloads a file <see cref="Stream"/> for a given <paramref name="ticket"/>.
		/// </summary>
		/// <param name="ticket">The <see cref="FileTicketResponse"/> to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the downloaded <see cref="Stream"/>.</returns>
		ValueTask<Stream> Download(FileTicketResponse ticket, CancellationToken cancellationToken);

		/// <summary>
		/// Uploads a given <paramref name="uploadStream"/> for a given <paramref name="ticket"/>.
		/// </summary>
		/// <param name="ticket">The <see cref="FileTicketResponse"/> to download.</param>
		/// <param name="uploadStream">The <see cref="Stream"/> to upload. <see langword="null"/> represents an empty file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Upload(FileTicketResponse ticket, Stream? uploadStream, CancellationToken cancellationToken);
	}
}
