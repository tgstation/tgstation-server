using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Caches the <see cref="Stream"/> from a <see cref="HttpResponseMessage"/> for later use.
	/// </summary>
	sealed class CachedResponseStream : Stream
	{
		/// <summary>
		/// The <see cref="HttpResponseMessage"/> for the <see cref="CachedResponseStream"/>.
		/// </summary>
		readonly HttpResponseMessage response;

		/// <summary>
		/// The reponse content <see cref="Stream"/>.
		/// </summary>
		readonly Stream responseStream;

		/// <summary>
		/// Asyncronously creates a new <see cref="CachedResponseStream"/>.
		/// </summary>
		/// <param name="response">The <see cref="HttpResponseMessage"/> to build from.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="CachedResponseStream"/>.</returns>
		public static async Task<CachedResponseStream> Create(HttpResponseMessage response)
		{
			var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
			return new CachedResponseStream(response, stream);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedResponseStream"/> <see langword="class"/>.
		/// </summary>
		/// <param name="response">The value of <see cref="response"/>.</param>
		/// <param name="responseStream">The value of <see cref="responseStream"/>.</param>
		CachedResponseStream(HttpResponseMessage response, Stream responseStream)
		{
			this.response = response;
			this.responseStream = responseStream;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing)
				return;
			responseStream.Dispose();
			response.Dispose();
		}

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			await base.DisposeAsync().ConfigureAwait(false);
			await responseStream.DisposeAsync().ConfigureAwait(false);
			response.Dispose();
		}

		/// <inheritdoc />
		public override bool CanRead => responseStream.CanRead;

		/// <inheritdoc />
		public override bool CanSeek => responseStream.CanSeek;

		/// <inheritdoc />
		public override bool CanWrite => responseStream.CanWrite;

		/// <inheritdoc />
		public override long Length => responseStream.Length;

		/// <inheritdoc />
		public override long Position
		{
			get => responseStream.Position;
			set => responseStream.Position = value;
		}

		/// <inheritdoc />
		public override void Flush() => responseStream.Flush();

		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count) => responseStream.Read(buffer, offset, count);

		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin) => responseStream.Seek(offset, origin);

		/// <inheritdoc />
		public override void SetLength(long value) => responseStream.SetLength(value);

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count) => responseStream.Write(buffer, offset, count);
	}
}
