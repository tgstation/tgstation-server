using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Extensions;
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
				if (expireTask != null)
				{
					disposeCts.Cancel();
					disposeCts.Dispose();
					toAwait = expireTask;
					expireTask = null;
				}
				else
					toAwait = Task.CompletedTask;

			await toAwait;
		}

		/// <inheritdoc />
		public FileTicketResponse CreateDownload(FileDownloadProvider downloadProvider)
		{
			if (downloadProvider == null)
				throw new ArgumentNullException(nameof(downloadProvider));

			logger.LogDebug("Creating download ticket for path {filePath}", downloadProvider.FilePath);
			var ticketResult = CreateTicket();

			lock (downloadTickets)
				downloadTickets.Add(ticketResult.FileTicket, downloadProvider);

			QueueExpiry(() =>
			{
				lock (downloadTickets)
					if (downloadTickets.Remove(ticketResult.FileTicket))
						logger.LogTrace("Expired download ticket {ticket}...", ticketResult.FileTicket);
			});

			logger.LogTrace("Created download ticket {ticket}", ticketResult.FileTicket);

			return ticketResult;
		}

		/// <inheritdoc />
		public IFileUploadTicket CreateUpload(bool requireSynchronousIO)
		{
			logger.LogDebug("Creating upload ticket...");
			var uploadTicket = new FileUploadProvider(CreateTicket(), requireSynchronousIO);

			lock (uploadTickets)
				uploadTickets.Add(uploadTicket.Ticket.FileTicket, uploadTicket);

			QueueExpiry(() =>
			{
				lock (uploadTickets)
					if (uploadTickets.Remove(uploadTicket.Ticket.FileTicket))
						logger.LogTrace("Expired upload ticket {ticket}...", uploadTicket.Ticket.FileTicket);
					else
						return;

				uploadTicket.Expire();
			});

			logger.LogTrace("Created upload ticket {ticket}", uploadTicket.Ticket.FileTicket);

			return uploadTicket;
		}

		/// <inheritdoc />
		public async Task<Tuple<FileStream, ErrorMessageResponse>> RetrieveDownloadStream(FileTicketResponse ticket, CancellationToken cancellationToken)
		{
			if (ticket == null)
				throw new ArgumentNullException(nameof(ticket));

			FileDownloadProvider downloadProvider;
			lock (downloadTickets)
			{
				if (!downloadTickets.TryGetValue(ticket.FileTicket, out downloadProvider))
				{
					logger.LogTrace("Download ticket {ticket} not found!", ticket.FileTicket);
					return Tuple.Create<FileStream, ErrorMessageResponse>(null, null);
				}

				downloadTickets.Remove(ticket.FileTicket);
			}

			var errorCode = downloadProvider.ActivationCallback();
			if (errorCode.HasValue)
			{
				logger.LogDebug("Download ticket {ticket} failed activation!", ticket.FileTicket);
				return Tuple.Create<FileStream, ErrorMessageResponse>(null, new ErrorMessageResponse(errorCode.Value));
			}

			FileStream stream;
			try
			{
				if (downloadProvider.FileStreamProvider != null)
					stream = await downloadProvider.FileStreamProvider(cancellationToken);
				else
					stream = ioManager.GetFileStream(downloadProvider.FilePath, downloadProvider.ShareWrite);
			}
			catch (IOException ex)
			{
				return Tuple.Create<FileStream, ErrorMessageResponse>(
					null,
					new ErrorMessageResponse(ErrorCode.IOError)
					{
						AdditionalData = ex.ToString(),
					});
			}

			try
			{
				logger.LogTrace("Ticket {ticket} downloading...", ticket.FileTicket);
				return Tuple.Create<FileStream, ErrorMessageResponse>(stream, null);
			}
			catch
			{
				stream.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async Task<ErrorMessageResponse> SetUploadStream(FileTicketResponse ticket, Stream stream, CancellationToken cancellationToken)
		{
			if (ticket == null)
				throw new ArgumentNullException(nameof(ticket));

			FileUploadProvider uploadProvider;
			lock (uploadTickets)
			{
				if (!uploadTickets.TryGetValue(ticket.FileTicket, out uploadProvider))
				{
					logger.LogTrace("Upload ticket {ticket} not found!", ticket.FileTicket);
					return new ErrorMessageResponse(ErrorCode.ResourceNotPresent);
				}

				uploadTickets.Remove(ticket.FileTicket);
			}

			return await uploadProvider.Completion(stream, cancellationToken);
		}

		/// <summary>
		/// Creates a new <see cref="FileTicketResponse"/>.
		/// </summary>
		/// <returns>A new <see cref="FileTicketResponse"/>.</returns>
		FileTicketResponse CreateTicket() => new ()
		{
			FileTicket = cryptographySuite.GetSecureString(),
		};

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
					await oldExpireTask.WithToken(disposeCts.Token);

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
