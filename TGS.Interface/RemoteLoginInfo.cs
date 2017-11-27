using System;

namespace TGS.Interface
{
	/// <summary>
	/// Information representing a remote server connection
	/// </summary>
	[Serializable]
	public sealed class RemoteLoginInfo
	{
		/// <summary>
		/// The IP address or URL of the target server
		/// </summary>
		public string IP { get; }
		/// <summary>
		/// The port to connect to the target server
		/// </summary>
		public ushort Port { get; }
		/// <summary>
		/// A Windows username for the target server
		/// </summary>
		public string Username { get; }
		/// <summary>
		/// The Windows password for <see cref="Username"/>
		/// </summary>
		internal string Password { get; }

		/// <summary>
		/// Backing field for <see cref="IP"/>
		/// </summary>
		readonly string _ip;
		/// <summary>
		/// Backing field for <see cref="Port"/>
		/// </summary>
		readonly ushort _port;
		/// <summary>
		/// Backing field for <see cref="Username"/>
		/// </summary>
		readonly string _username;
		/// <summary>
		/// Backing field for <see cref="Password"/>
		/// </summary>
		readonly string _password;

		/// <summary>
		/// Construct a <see cref="RemoteLoginInfo"/>
		/// </summary>
		/// <param name="ip">The value for <see cref="IP"/></param>
		/// <param name="port">The value for <see cref="Port"/></param>
		/// <param name="username">The value for <see cref="Username"/></param>
		/// <param name="password">The value for <see cref="Password"/></param>
		public RemoteLoginInfo(string ip, ushort port, string username, string password)
		{
			_ip = ip;
			_port = port;
			_username = username;
			_password = password;
		}
	}
}
