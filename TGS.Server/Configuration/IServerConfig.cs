using System.Collections.Generic;
using TGS.Server.IO;

namespace TGS.Server.Configuration
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
		/// List of paths that contain <see cref="Components.Instance"/>s
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
		/// <param name="IO">The <see cref="IIOManager"/> to use</param>
		void Save(IIOManager IO);

		/// <summary>
		/// Saves the <see cref="ServerConfig"/> to a target <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The directory in which to save the <see cref="ServerConfig"/></param>
		/// <param name="IO">The <see cref="IIOManager"/> to use</param>
		void Save(string directory, IIOManager IO);
	}
}
