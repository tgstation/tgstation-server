using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Transfer
{
	/// <summary>
	/// Represents a file on disk to be downloaded.
	/// </summary>
	public sealed class FileDownloadProvider
	{
		/// <summary>
		/// A <see cref="Func{TResult}"/> to run before providing the download. If it returns a non-null <see cref="ErrorCode"/>, a 400 error with that code will be returned instead of a download stream.
		/// </summary>
		public Func<ErrorCode?> ActivationCallback { get; }

		/// <summary>
		/// A <see cref="Func{T, TResult}"/> to specially provide a <see cref="Task{TResult}"/> returning the <see cref="Stream"/> of the file download. The caller will own the resulting <see cref="Stream"/>.
		/// </summary>
		public Func<CancellationToken, Task<Stream>> StreamProvider { get; }

		/// <summary>
		/// The full path to the file on disk to download.
		/// </summary>
		public string FilePath { get; }

		/// <summary>
		/// If the file read stream should be allowed to share writes. If this is set, the entire file will be buffered to avoid Content-Length mismatches.
		/// </summary>
		public bool ShareWrite { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDownloadProvider"/> class.
		/// </summary>
		/// <param name="activationCallback">The value of <see cref="ActivationCallback"/>.</param>
		/// <param name="streamProvider">The optional value of <see cref="StreamProvider"/>.</param>
		/// <param name="filePath">The value of <see cref="FilePath"/>.</param>
		/// <param name="shareWrite">The value of <see cref="ShareWrite"/>.</param>
		public FileDownloadProvider(
			Func<ErrorCode?> activationCallback,
			Func<CancellationToken, Task<Stream>> streamProvider,
			string filePath,
			bool shareWrite)
		{
			ActivationCallback = activationCallback ?? throw new ArgumentNullException(nameof(activationCallback));
			StreamProvider = streamProvider;
			FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
			ShareWrite = shareWrite;
		}
	}
}
