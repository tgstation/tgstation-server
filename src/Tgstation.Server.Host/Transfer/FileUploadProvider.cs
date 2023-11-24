using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.WebUtilities;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
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
		readonly TaskCompletionSource<Stream?> streamTcs;

		/// <summary>
		/// The <see cref="TaskCompletionSource"/> that completes in <see cref="IDisposable.Dispose"/> or when <see cref="SetError(ErrorCode, string)"/> is called.
		/// </summary>
		readonly TaskCompletionSource completionTcs;

		/// <summary>
		/// Determines the backing for the <see cref="Stream"/> returned from <see cref="GetResult(CancellationToken)"/>.
		/// </summary>
		readonly FileUploadStreamKind streamKind;

		/// <summary>
		/// The <see cref="ErrorMessageResponse"/> that occurred while processing the upload if any.
		/// </summary>
		ErrorMessageResponse? errorMessage;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileUploadProvider"/> class.
		/// </summary>
		/// <param name="ticket">The value of <see cref="Ticket"/>.</param>
		/// <param name="streamKind">The value of <see cref="streamKind"/>.</param>
		public FileUploadProvider(FileTicketResponse ticket, FileUploadStreamKind streamKind)
		{
			Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));

			ticketExpiryCts = new CancellationTokenSource();
			streamTcs = new TaskCompletionSource<Stream?>();
			completionTcs = new TaskCompletionSource();
			this.streamKind = streamKind;
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync()
		{
			ticketExpiryCts.Dispose();
			completionTcs.TrySetResult();
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public async ValueTask<Stream?> GetResult(CancellationToken cancellationToken)
		{
			using (cancellationToken.Register(() => streamTcs.TrySetCanceled(cancellationToken)))
			using (ticketExpiryCts.Token.Register(() => streamTcs.TrySetResult(null)))
				return await streamTcs.Task;
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
		public async ValueTask<ErrorMessageResponse?> Completion(Stream stream, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(stream);

			if (ticketExpiryCts.IsCancellationRequested)
				return new ErrorMessageResponse(ErrorCode.ResourceNotPresent);

			Stream? bufferedStream = null;
			try
			{
				switch (streamKind)
				{
					case FileUploadStreamKind.ForSynchronousIO:
						// big reads, we should buffer to disk
						// NOTE: We do this here ONLY because we can't use async methods in a synchronous context
						// Ideally we should be doing 0 buffering here, but this is a compromise
						bufferedStream = new FileBufferingReadStream(stream, DefaultIOManager.DefaultBufferSize);
						await bufferedStream.DrainAsync(cancellationToken);
						break;
					case FileUploadStreamKind.None:
						break;
					default:
						throw new InvalidOperationException($"Invalid FileUploadStreamKind: {streamKind}");
				}
			}
			catch
			{
				if (bufferedStream != null)
					await bufferedStream.DisposeAsync();

				throw;
			}

			await using (bufferedStream)
			{
				streamTcs.TrySetResult(bufferedStream ?? stream);

				await completionTcs.Task.WaitAsync(cancellationToken);
				return errorMessage;
			}
		}

		/// <inheritdoc />
		public void SetError(ErrorCode errorCode, string additionalData)
		{
			if (errorMessage != null)
				throw new InvalidOperationException("Error already set!");

			errorMessage = new ErrorMessageResponse(errorCode)
			{
				AdditionalData = additionalData,
			};
			completionTcs.TrySetResult();
		}
	}
}
