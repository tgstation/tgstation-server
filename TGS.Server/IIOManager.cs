using System.Collections.Generic;

namespace TGS.Server
{
	/// <summary>
	/// Interface for managing files ala <see cref="System.IO.File"/> and <see cref="System.IO.Directory"/>
	/// </summary>
	interface IIOManager
	{
		/// <summary>
		/// Retrieve the full path of some <paramref name="path"/>
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		string ResolvePath(string path);
		string ReadAllText(string path);
		void WriteAllText(string path, string contents);
		void AppendAllText(string path, string additional_contents);
		byte[] ReadAllBytes(string path);
		void WriteAllBytes(string path, byte[] contents);
		bool FileExists(string path);
		void DeleteFile(string path);
		void CopyFile(string src, string dest, bool overwrite);
		void MoveFile(string src, string dest);
		void CreateDirectory(string path);
		bool DirectoryExists(string path);
		void DeleteDirectory(string path, bool ContentsOnly = false, IEnumerable<string> excludeRoot = null);
		void CopyDirectory(string src, string dest, IEnumerable<string> ignore = null, bool ignoreIfNotExists = false);
		void MoveDirectory(string src, string dest);
		void CreateSymlink(string link, string target);
	}
}
