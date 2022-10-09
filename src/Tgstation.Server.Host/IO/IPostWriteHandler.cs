namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// Handles changing file modes/permissions after writing.
	/// </summary>
	interface IPostWriteHandler
	{
		/// <summary>
		/// Check if a given <paramref name="sourceFilePath"/> will need <see cref="HandleWrite(string)"/> called on a copy of it.
		/// </summary>
		/// <param name="sourceFilePath">The path of the source file to check.</param>
		/// <returns><see langword="true"/> if <see cref="HandleWrite(string)"/> should be called on copies of <paramref name="sourceFilePath"/>, <see langword="false"/> otherwise.</returns>
		public bool NeedsPostWrite(string sourceFilePath);

		/// <summary>
		/// For handling system specific necessities after a write.
		/// </summary>
		/// <param name="filePath">The full path to the file that was written.</param>
		void HandleWrite(string filePath);
	}
}
