using Microsoft.AspNetCore.WebUtilities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Transfer
{
	/// <inheritdoc />
	sealed class FileUploadProvider : IFileUploadTicket
	{
		/// <inheritdoc />
		public FileTicketResponse Ticket { get; }

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the ticket duration.
		/// </summary>
		readonly CancellationTokenSource ticketExpiryCts;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> for the <see cref="Stream"/>.
		/// </summary>
		readonly TaskCompletionSource<Stream> taskCompletionSource;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> that completes in <see cref="IDisposable.Dispose"/> or when <see cref="SetErrorMessage(ErrorMessageResponse)"/> is called.
		/// </summary>
		readonly TaskCompletionSource<object> completionTcs;

		/// <summary>
		/// If synchronous IO is required. Uses a <see cref="FileBufferingReadStream"/> as a backend if set.
		/// </summary>
		readonly bool requireSynchronousIO;

		/// <summary>
		/// The <see cref="ErrorMessageResponse"/> that occurred while processing the upload if any.
		/// </summary>
		ErrorMessageResponse errorMessage;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileUploadProvider"/> <see langword="class"/>.
		/// </summary>
		/// <param name="ticket">The value of <see cref="Ticket"/>.</param>
		/// <param name="requireSynchronousIO">The value of <see cref="requireSynchronousIO"/></param>
		public FileUploadProvider(FileTicketResponse ticket, bool requireSynchronousIO)
		{
			Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));

			ticketExpiryCts = new CancellationTokenSource();
			taskCompletionSource = new TaskCompletionSource<Stream>();
			completionTcs = new TaskCompletionSource<object>();
			this.requireSynchronousIO = requireSynchronousIO;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			ticketExpiryCts.Dispose();
			completionTcs.TrySetResult(null);
		}

		/// <inheritdoc />
		public async Task<Stream> GetResult(CancellationToken cancellationToken)
		{
			using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
			using (ticketExpiryCts.Token.Register(() => taskCompletionSource.TrySetResult(null)))
				return await taskCompletionSource.Task.ConfigureAwait(false);
		}

		/// <summary>
		/// Expire the <see cref="FileUploadProvider"/>.
		/// </summary>
		public void Expire()
		{
			if (!completionTcs.Task.IsCompleted)
				ticketExpiryCts.Cancel();
		}

		/// <summary>
		/// Resolve the <paramref name="stream"/> for the <see cref="FileUploadProvider"/> and awaits the upload.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> containing uploaded data.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="null"/>, <see cref="ErrorMessageResponse"/> otherwise.</returns>
		public async Task<ErrorMessageResponse> Completion(Stream stream, CancellationToken cancellationToken)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (ticketExpiryCts.IsCancellationRequested)
				return new ErrorMessageResponse(ErrorCode.ResourceNotPresent);

			Stream bufferedStream = null;
			if (requireSynchronousIO)
			{
				// big reads, we should buffer to disk
				bufferedStream = new FileBufferingReadStream(stream, DefaultIOManager.DefaultBufferSize);
				await bufferedStream.DrainAsync(cancellationToken).ConfigureAwait(false);
			}

			using (bufferedStream)
			{
				taskCompletionSource.TrySetResult(bufferedStream ?? stream);

				await completionTcs.Task.WithToken(cancellationToken).ConfigureAwait(false);
				return errorMessage;
			}
		}

		/// <inheritdoc />
		public void SetErrorMessage(ErrorMessageResponse errorMessage)
		{
			if (errorMessage == null)
#pragma warning disable IDE0016 // Use 'throw' expression
				throw new ArgumentNullException(nameof(errorMessage));
#pragma warning restore IDE0016 // Use 'throw' expression

			if (this.errorMessage != null)
				throw new InvalidOperationException("ErrorMessage already set!");

			this.errorMessage = errorMessage;
			completionTcs.TrySetResult(null);
		}
	}
}
