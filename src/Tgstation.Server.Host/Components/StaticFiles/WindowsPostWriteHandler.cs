namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <summary>
	/// <see cref="IPostWriteHandler"/> for Windows systems
	/// </summary>
	sealed class WindowsPostWriteHandler : IPostWriteHandler
	{
		/// <inheritdoc />
		public void HandleWrite(string filePath) { }
	}
}
