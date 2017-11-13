using System.Collections.Generic;

namespace TGS.Server
{
	/// <summary>
	/// Class for storing server wide settings
	/// </summary>
	public interface IServerConfig
	{
		/// <summary>
		/// The version of the <see cref="IServerConfig"/>
		/// </summary>
		ulong Version { get; }

		/// <summary>
		/// List of paths that contain <see cref="Instance"/>s
		/// </summary>
		IList<string> InstancePaths { get; }

		/// <summary>
		/// Port used to access the <see cref="Server"/> remotely
		/// </summary>
		ushort RemoteAccessPort { get; set; }

		/// <summary>
		/// Path to the directory containing the Python2.7 installation
		/// </summary>
		string PythonPath { get; set; }

		/// <summary>
		/// Saves the <see cref="ServerConfig"/> to the default directory
		/// </summary>
		void Save();

		/// <summary>
		/// Saves the <see cref="ServerConfig"/> to a target <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The directory in which to save the <see cref="ServerConfig"/></param>
		void Save(string directory);
	}
}
