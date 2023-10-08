using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tgstation.Server.Common.Http
{
	/// <summary>
	/// Caches the <see cref="Stream"/> from a <see cref="HttpResponseMessage"/> for later use.
	/// </summary>
	public sealed class CachedResponseStream : Stream
	{
		/// <summary>
		/// The <see cref="HttpContent"/> for the <see cref="CachedResponseStream"/>.
		/// </summary>
		readonly HttpContent responseContent;

		/// <summary>
		/// The reponse content <see cref="Stream"/>.
		/// </summary>
		readonly Stream responseStream;

		/// <summary>
		/// Asyncronously creates a new <see cref="CachedResponseStream"/>.
		/// </summary>
		/// <param name="response">The <see cref="HttpResponseMessage"/> to build from.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="CachedResponseStream"/>.</returns>
		public static async ValueTask<CachedResponseStream> Create(HttpResponseMessage response)
		{
			if (response == null)
				throw new ArgumentNullException(nameof(response));

			using (response)
			{
				var content = response.Content;
				response.Content = null;
				try
				{
					// don't cry about the missing CancellationToken overload: https://github.com/dotnet/runtime/issues/916
					var responseStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
					return new CachedResponseStream(content, responseStream);
				}
				catch
				{
					content.Dispose();
					throw;
				}
			}
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

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing)
				return;

			responseContent.Dispose();
			responseStream.Dispose();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedResponseStream"/> class.
		/// </summary>
		/// <param name="responseContent">The value of <see cref="responseContent"/>.</param>
		/// <param name="responseStream">The value of <see cref="responseStream"/>.</param>
		CachedResponseStream(HttpContent responseContent, Stream responseStream)
		{
			this.responseContent = responseContent;
			this.responseStream = responseStream;
		}
	}
}
