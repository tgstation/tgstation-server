using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;

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
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="FileTicketResult.FileTicket"/>s to upload <see cref="Stream"/> <see cref="TaskCompletionSource{TResult}"/>s.
		/// </summary>
		readonly Dictionary<string, FileUploadProvider> uploadTickets;

		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="FileTicketResult.FileTicket"/>s to <see cref="FileDownloadProvider"/>s.
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
		/// Initializes a new instance of the <see cref="FileTransferService"/> <see langword="class"/>.
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

			await toAwait.ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a new <see cref="FileTicketResult"/>.
		/// </summary>
		/// <returns>A new <see cref="FileTicketResult"/>.</returns>
		FileTicketResult CreateTicket() => new FileTicketResult
		{
			FileTicket = cryptographySuite.GetSecureString()
		};

		void QueueExpiry(Action expireAction)
		{
			Task oldExpireTask = null;

			async Task ExpireAsync()
			{
				var expireAt = DateTimeOffset.Now + TimeSpan.FromMinutes(TicketValidityMinutes);
				try
				{
					await oldExpireTask.WithToken(disposeCts.Token).ConfigureAwait(false);

					var now = DateTimeOffset.Now;
					if (now < expireAt)
						await asyncDelayer.Delay(expireAt - now, disposeCts.Token).ConfigureAwait(false);
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

		/// <inheritdoc />
		public FileTicketResult CreateDownload(FileDownloadProvider downloadProvider)
		{
			if (downloadProvider == null)
				throw new ArgumentNullException(nameof(downloadProvider));

			logger.LogDebug("Creating download ticket for path {0}", downloadProvider.FilePath);
			var ticketResult = CreateTicket();

			lock (downloadTickets)
				downloadTickets.Add(ticketResult.FileTicket, downloadProvider);

			QueueExpiry(() =>
			{
				logger.LogTrace("Expiring download ticket {0}...", ticketResult.FileTicket);
				lock (downloadTickets)
					downloadTickets.Remove(ticketResult.FileTicket);
			});

			logger.LogTrace("Created download ticket {0}", ticketResult.FileTicket);

			return ticketResult;
		}

		/// <inheritdoc />
		public IFileUploadTicket CreateUpload()
		{
			logger.LogDebug("Creating upload ticket...");
			var uploadTicket = new FileUploadProvider(CreateTicket());

			lock (uploadTickets)
				uploadTickets.Add(uploadTicket.Ticket.FileTicket, uploadTicket);

			QueueExpiry(() =>
			{
				logger.LogTrace("Expiring upload ticket {0}...", uploadTicket.Ticket.FileTicket);
				lock (uploadTickets)
					uploadTickets.Remove(uploadTicket.Ticket.FileTicket);

				uploadTicket.Expire();
			});

			logger.LogTrace("Created upload ticket {0}", uploadTicket.Ticket.FileTicket);

			return uploadTicket;
		}

		/// <inheritdoc />
		public async Task<Tuple<Stream, ErrorMessage>> RetrieveDownloadStream(FileTicketResult ticket, CancellationToken cancellationToken)
		{
			if (ticket == null)
				throw new ArgumentNullException(nameof(ticket));

			FileDownloadProvider downloadProvider;
			lock (downloadTickets)
			{
				if (!downloadTickets.TryGetValue(ticket.FileTicket, out downloadProvider))
				{
					logger.LogTrace("Download ticket {0} not found!", ticket.FileTicket);
					return Tuple.Create<Stream, ErrorMessage>(null, null);
				}

				downloadTickets.Remove(ticket.FileTicket);
			}

			var errorCode = await downloadProvider.ActivationCallback(cancellationToken).ConfigureAwait(false);
			if (errorCode.HasValue)
			{
				logger.LogDebug("Download ticket {0} failed activation!", ticket.FileTicket);
				return Tuple.Create<Stream, ErrorMessage>(null, new ErrorMessage(errorCode.Value));
			}

			Stream stream;
			try
			{
				stream = ioManager.GetFileStream(downloadProvider.FilePath, downloadProvider.ShareWrite);
			}
			catch (IOException ex)
			{
				return Tuple.Create<Stream, ErrorMessage>(
					null,
					new ErrorMessage(ErrorCode.IOError)
					{
						AdditionalData = ex.ToString()
					});
			}

			try
			{
				logger.LogTrace("Ticket {0} downloading...", ticket.FileTicket);
				return Tuple.Create<Stream, ErrorMessage>(stream, null);
			}
			catch
			{
				stream.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async Task<ErrorMessage> SetUploadStream(FileTicketResult ticket, Stream stream, CancellationToken cancellationToken)
		{
			if (ticket == null)
				throw new ArgumentNullException(nameof(ticket));

			FileUploadProvider uploadProvider;
			lock (uploadTickets)
			{
				if (!uploadTickets.TryGetValue(ticket.FileTicket, out uploadProvider))
				{
					logger.LogTrace("Upload ticket {0} not found!", ticket.FileTicket);
					return new ErrorMessage(ErrorCode.ResourceNotPresent);
				}

				uploadTickets.Remove(ticket.FileTicket);
			}

			return await uploadProvider.Completion(stream, cancellationToken).ConfigureAwait(false);
		}
	}
}
