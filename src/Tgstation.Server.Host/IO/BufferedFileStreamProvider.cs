using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IFileStreamProvider"/> that provides a <see cref="ISeekableFileStreamProvider"/> from an input <see cref="Stream"/>.
	/// </summary>
	public sealed class BufferedFileStreamProvider : ISeekableFileStreamProvider
	{
		/// <inheritdoc />
		public bool Disposed => buffer == null;

		/// <summary>
		/// The original input <see cref="Stream"/> must remain valid for the lifetime of the <see cref="BufferedFileStreamProvider"/> or until <see cref="GetResult(CancellationToken)"/> first returns.
		/// </summary>
		readonly Stream input;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> used to synchronize writes to <see cref="buffer"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The backing <see cref="Stream"/>.
		/// </summary>
		volatile MemoryStream buffer;

		/// <summary>
		/// If <see cref="buffer"/> has been populated.
		/// </summary>
		volatile bool buffered;

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferedFileStreamProvider"/> class.
		/// </summary>
		/// <param name="input">The value of <see cref="input"/>.</param>
		public BufferedFileStreamProvider(Stream input)
		{
			this.input = input ?? throw new ArgumentNullException(nameof(input));

			semaphore = new SemaphoreSlim(1);
			try
			{
				buffer = new MemoryStream();
			}
			catch
			{
				semaphore.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			MemoryStream localBuffer;
			lock (semaphore)
			{
				localBuffer = buffer;
				if (localBuffer == null)
					return;

				// important to drop the reference so it can properly GC
				// The implementation of MemoryStream doesn't fucking do this for some reason
				buffer = null;
				buffered = true;
			}

			var bufferDispose = localBuffer.DisposeAsync();
			semaphore.Dispose();
			await bufferDispose;
		}

		/// <inheritdoc />
		public async Task<Stream> GetResult(CancellationToken cancellationToken)
		{
			var (sharedStream, _) = await GetResultInternal(cancellationToken);
			return sharedStream;
		}

		/// <inheritdoc />
		public async Task<MemoryStream> GetOwnedResult(CancellationToken cancellationToken)
		{
			var (sharedStream, length) = await GetResultInternal(cancellationToken);
			return new MemoryStream(sharedStream.GetBuffer(), 0, (int)length, false, true);
		}

		/// <summary>
		/// Gets the shared <see cref="MemoryStream"/> and its <see cref="Stream.Length"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see cref="buffer"/> and its <see cref="Stream.Length"/>.</returns>
		async Task<(MemoryStream, long)> GetResultInternal(CancellationToken cancellationToken)
		{
			if (!buffered)
				using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken))
					if (!buffered)
					{
						MemoryStream localBuffer;
						lock (semaphore)
							localBuffer = buffer ?? throw new ObjectDisposedException(nameof(BufferedFileStreamProvider));

						await input.CopyToAsync(localBuffer, cancellationToken);
						localBuffer.Seek(0, SeekOrigin.Begin);
						buffered = true;
						return (localBuffer, localBuffer.Length);
					}

			lock (semaphore)
			{
				var localBuffer = buffer ?? throw new ObjectDisposedException(nameof(BufferedFileStreamProvider));
				return (
					localBuffer,
					localBuffer.Length);
			}
		}
	}
}
