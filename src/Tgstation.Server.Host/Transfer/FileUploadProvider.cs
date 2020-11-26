using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Transfer
{
	/// <inheritdoc />
	sealed class FileUploadProvider : IFileUploadTicket
	{
		/// <inheritdoc />
		public FileTicketResult Ticket { get; }

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the ticket duration.
		/// </summary>
		readonly CancellationTokenSource ticketExpiryCts;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> for the <see cref="Stream"/>.
		/// </summary>
		readonly TaskCompletionSource<Stream> taskCompletionSource;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> that completes in <see cref="IDisposable.Dispose"/> or when <see cref="SetErrorMessage(ErrorMessage)"/> is called.
		/// </summary>
		readonly TaskCompletionSource<object> completionTcs;

		/// <summary>
		/// The <see cref="ErrorMessage"/> that occurred while processing the upload if any.
		/// </summary>
		ErrorMessage errorMessage;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileUploadProvider"/> <see langword="class"/>.
		/// </summary>
		/// <param name="ticket">The value of <see cref="Ticket"/>.</param>
		public FileUploadProvider(FileTicketResult ticket)
		{
			Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));

			ticketExpiryCts = new CancellationTokenSource();
			taskCompletionSource = new TaskCompletionSource<Stream>();
			completionTcs = new TaskCompletionSource<object>();
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="null"/>, <see cref="ErrorMessage"/> otherwise.</returns>
		public async Task<ErrorMessage> Completion(Stream stream, CancellationToken cancellationToken)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (ticketExpiryCts.IsCancellationRequested)
				return new ErrorMessage(ErrorCode.ResourceNotPresent);

			taskCompletionSource.TrySetResult(stream);

			await completionTcs.Task.WithToken(cancellationToken).ConfigureAwait(false);
			return errorMessage;
		}

		/// <inheritdoc />
		public void SetErrorMessage(ErrorMessage errorMessage)
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
