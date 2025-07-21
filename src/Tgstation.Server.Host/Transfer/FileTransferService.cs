using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// Implementation of the file transfer service.
	/// </summary>
	sealed class FileTransferService : IFileTransferTicketProvider, IFileTransferStreamHandler, IAsyncDisposable
	{
		/// <summary>
		/// Number of minutes before transfer ticket expire.
		/// </summary>
		const int TicketValidityMinutes = 5;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="FileTransferService"/>.
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="FileTransferService"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="FileTransferService"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="FileTransferService"/>.
		/// </summary>
		readonly ILogger<FileTransferService> logger;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="FileTicketResponse.FileTicket"/>s to upload <see cref="Stream"/> <see cref="TaskCompletionSource{TResult}"/>s.
		/// </summary>
		readonly Dictionary<string, FileUploadProvider> uploadTickets;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="FileTicketResponse.FileTicket"/>s to <see cref="FileDownloadProvider"/>s.
		/// </summary>
		readonly Dictionary<string, FileDownloadProvider> downloadTickets;

		/// <summary>
		/// <see cref="CancellationTokenSource"/> that is triggered when <see cref="IAsyncDisposable.DisposeAsync"/> is called.
		/// </summary>
		readonly CancellationTokenSource disposeCts;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> used to update <see cref="expireTask"/>.
		/// </summary>
		readonly object synchronizationLock;

		/// <summary>
		/// Combined <see cref="Task"/> of all <see cref="QueueExpiry(Action)"/> calls.
		/// </summary>
		Task expireTask;

		/// <summary>
		/// If the <see cref="FileTransferService"/> is disposed.
		/// </summary>
		bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileTransferService"/> class.
		/// </summary>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public FileTransferService(
			ICryptographySuite cryptographySuite,
			IIOManager ioManager,
			IAsyncDelayer asyncDelayer,
			ILogger<FileTransferService> logger)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			uploadTickets = new Dictionary<string, FileUploadProvider>();
			downloadTickets = new Dictionary<string, FileDownloadProvider>();

			disposeCts = new CancellationTokenSource();

			expireTask = Task.CompletedTask;
			synchronizationLock = new object();
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			Task toAwait;
			lock (synchronizationLock)
				if (!disposed)
				{
					disposeCts.Cancel();
					disposeCts.Dispose();
					disposed = true;
					toAwait = expireTask;
					expireTask = Task.CompletedTask;
				}
				else
					toAwait = Task.CompletedTask;

			await toAwait;
		}

		/// <inheritdoc />
		public FileTicketResponse CreateDownload(FileDownloadProvider downloadProvider)
		{
			ArgumentNullException.ThrowIfNull(downloadProvider);
			ObjectDisposedException.ThrowIf(disposed, this);

			logger.LogDebug("Creating download ticket for path {filePath}", downloadProvider.FilePath);
			var ticket = cryptographySuite.GetSecureString();

			lock (downloadTickets)
				downloadTickets.Add(ticket, downloadProvider);

			QueueExpiry(() =>
			{
				lock (downloadTickets)
					if (downloadTickets.Remove(ticket))
						logger.LogTrace("Expired download ticket {ticket}...", ticket);
			});

			logger.LogTrace("Created download ticket {ticket}", ticket);

			return new FileTicketResponse
			{
				FileTicket = ticket,
			};
		}

		/// <inheritdoc />
		public IFileUploadTicket CreateUpload(FileUploadStreamKind streamKind)
		{
			ObjectDisposedException.ThrowIf(disposed, this);

			logger.LogDebug("Creating upload ticket...");
			var ticket = cryptographySuite.GetSecureString();
			var uploadTicket = new FileUploadProvider(
				new FileTicketResponse
				{
					FileTicket = ticket,
				},
				streamKind);

			lock (uploadTickets)
				uploadTickets.Add(ticket, uploadTicket);

			QueueExpiry(() =>
			{
				lock (uploadTickets)
					if (uploadTickets.Remove(ticket))
						logger.LogTrace("Expired upload ticket {ticket}...", ticket);
					else
						return;

				uploadTicket.Expire();
			});

			logger.LogTrace("Created upload ticket {ticket}", ticket);

			return uploadTicket;
		}

		/// <inheritdoc />
		public async ValueTask<Tuple<Stream?, ErrorMessageResponse?>> RetrieveDownloadStream(FileTicketResponse ticketResponse, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ticketResponse);
			ObjectDisposedException.ThrowIf(disposed, this);

			var ticket = ticketResponse.FileTicket ?? throw new InvalidOperationException("ticketResponse must have FileTicket!");
			FileDownloadProvider? downloadProvider;
			lock (downloadTickets)
			{
				if (!downloadTickets.TryGetValue(ticket, out downloadProvider))
				{
					logger.LogTrace("Download ticket {ticket} not found!", ticket);
					return Tuple.Create<Stream?, ErrorMessageResponse?>(null, null);
				}

				downloadTickets.Remove(ticket);
			}

			var errorCode = downloadProvider.ActivationCallback();
			if (errorCode.HasValue)
			{
				logger.LogDebug("Download ticket {ticket} failed activation!", ticket);
				return Tuple.Create<Stream?, ErrorMessageResponse?>(null, new ErrorMessageResponse(errorCode.Value));
			}

			Stream stream;
			try
			{
				if (downloadProvider.StreamProvider != null)
					stream = await downloadProvider.StreamProvider(cancellationToken);
				else
					stream = ioManager.CreateAsyncReadStream(downloadProvider.FilePath, false, downloadProvider.ShareWrite);
			}
			catch (IOException ex)
			{
				return Tuple.Create<Stream?, ErrorMessageResponse?>(
					null,
					new ErrorMessageResponse(ErrorCode.IOError)
					{
						AdditionalData = ex.ToString(),
					});
			}

			try
			{
				logger.LogTrace("Ticket {ticket} downloading...", ticket);
				return Tuple.Create<Stream?, ErrorMessageResponse?>(stream, null);
			}
			catch
			{
				await stream.DisposeAsync();
				throw;
			}
		}

		/// <inheritdoc />
		public async ValueTask<ErrorMessageResponse?> SetUploadStream(FileTicketResponse ticketResponse, Stream stream, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(ticketResponse);
			ObjectDisposedException.ThrowIf(disposed, this);

			var ticket = ticketResponse.FileTicket ?? throw new InvalidOperationException("ticketResponse must have FileTicket!");
			FileUploadProvider? uploadProvider;
			lock (uploadTickets)
			{
				if (!uploadTickets.TryGetValue(ticket, out uploadProvider))
				{
					logger.LogTrace("Upload ticket {ticket} not found!", ticket);
					return new ErrorMessageResponse(ErrorCode.ResourceNotPresent);
				}

				uploadTickets.Remove(ticket);
			}

			return await uploadProvider.Completion(stream, cancellationToken);
		}

		/// <summary>
		/// Queue an <paramref name="expireAction"/> to run after <see cref="TicketValidityMinutes"/>.
		/// </summary>
		/// <param name="expireAction">The <see cref="Action"/> to take after <see cref="TicketValidityMinutes"/>.</param>
		void QueueExpiry(Action expireAction)
		{
			Task oldExpireTask;
			async Task ExpireAsync()
			{
				var expireAt = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(TicketValidityMinutes);
				try
				{
					await oldExpireTask.WaitAsync(disposeCts.Token);

					var now = DateTimeOffset.UtcNow;
					if (now < expireAt)
						await asyncDelayer.Delay(expireAt - now, disposeCts.Token);
				}
				finally
				{
					expireAction();
				}
			}

			lock (synchronizationLock)
			{
				oldExpireTask = expireTask;
				expireTask = ExpireAsync();
			}
		}
	}
}
