using System;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IPostWriteHandler"/> for Windows systems.
	/// </summary>
	sealed class WindowsPostWriteHandler : IPostWriteHandler
	{
		/// <inheritdoc />
		public bool NeedsPostWrite(string sourceFilePath)
		{
			if (sourceFilePath == null)
				throw new ArgumentNullException(nameof(sourceFilePath));

			return false;
		}

		/// <inheritdoc />
		public void HandleWrite(string filePath)
		{
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));
		}
	}
}
