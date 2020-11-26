using System;
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
		/// A <see cref="Func{T, TResult}"/> of a <see cref="Task{TResult}"/> to run before providing the download. If it returns a non-null <see cref="ErrorCode"/>, a 400 error with that code will be returned instead of a download stream.
		/// </summary>
		public Func<CancellationToken, Task<ErrorCode?>> ActivationCallback { get; }

		/// <summary>
		/// The full path to the file on disk to download.
		/// </summary>
		public string FilePath { get; }

		/// <summary>
		/// If the file read stream should be allowed to share writes.
		/// </summary>
		public bool ShareWrite { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDownloadProvider"/> <see langword="class"/>.
		/// </summary>
		/// <param name="activationCallback">The value of <see cref="ActivationCallback"/>.</param>
		/// <param name="filePath">The value of <see cref="FilePath"/>.</param>
		/// <param name="shareWrite">The value of <see cref="ShareWrite"/>.</param>
		public FileDownloadProvider(Func<CancellationToken, Task<ErrorCode?>> activationCallback, string filePath, bool shareWrite)
		{
			ActivationCallback = activationCallback ?? throw new ArgumentNullException(nameof(activationCallback));
			FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
			ShareWrite = shareWrite;
		}
	}
}
