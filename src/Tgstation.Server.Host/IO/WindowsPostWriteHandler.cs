using System;

#nullable disable

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
			ArgumentNullException.ThrowIfNull(sourceFilePath);

			return false;
		}

		/// <inheritdoc />
		public void HandleWrite(string filePath)
		{
			ArgumentNullException.ThrowIfNull(filePath);
		}
	}
}
