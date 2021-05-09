using System;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// <see cref="IPostWriteHandler"/> for Windows systems.
	/// </summary>
	sealed class WindowsPostWriteHandler : IPostWriteHandler
	{
		/// <inheritdoc />
		public void HandleWrite(string filePath)
		{
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));
		}
	}
}
