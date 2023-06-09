using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IFileStreamProvider"/> that buffers an input <see cref="Stream"/> in memory.
	/// </summary>
	sealed class BufferedFileStreamProvider : IFileStreamProvider
	{
		/// <summary>
		/// The input <see cref="Stream"/>.
		/// </summary>
		readonly Stream input;

		/// <summary>
		/// The backing <see cref="MemoryStream"/>.
		/// </summary>
		readonly MemoryStream buffer;

		/// <summary>
		/// Used to ensure exclusive access when writing to the <see cref="buffer"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// If the <see cref="input"/> has been drained into <see cref="buffer"/>.
		/// </summary>
		bool buffered;

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferedFileStreamProvider"/> class.
		/// </summary>
		/// <param name="input">The input <see cref="Stream"/>. Will not be fully buffered until <see cref="GetResult(CancellationToken)"/> completes.</param>
		public BufferedFileStreamProvider(Stream input)
		{
			this.input = input ?? throw new ArgumentNullException(nameof(input));
			buffer = new MemoryStream();
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public async Task<Stream> GetResult(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken))
			{
				if (!buffered)
				{
					await input.CopyToAsync(buffer, cancellationToken);
					buffer.Seek(0, SeekOrigin.Begin);
					buffered = true;
				}

				return buffer;
			}
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			var disposeTask = buffer.DisposeAsync();
			semaphore.Dispose();
			await disposeTask;
		}
	}
}
