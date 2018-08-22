namespace Tgstation.Server.Host.Components.StaticFiles
{
	interface IPostWriteHandler
	{
		/// <summary>
		/// For handling system specific necessities after a write
		/// </summary>
		/// <param name="filePath">The full path to the file that was written</param>
		void HandleWrite(string filePath);
	}
}
