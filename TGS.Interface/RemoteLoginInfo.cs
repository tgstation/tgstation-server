using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TGS.Interface
{
	/// <summary>
	/// Information representing a remote server connection
	/// </summary>
	public sealed class RemoteLoginInfo : IEquatable<RemoteLoginInfo>
	{
		/// <summary>
		/// Used for <see cref="Password"/> serialization
		/// </summary>
		const string EntropyFormatter = "{0}Entropy";

		/// <summary>
		/// The IP address or URL of the target server
		/// </summary>
		public string IP { get { return _ip; } }
		/// <summary>
		/// The port to connect to the target server
		/// </summary>
		public ushort Port { get { return _port; } }
		/// <summary>
		/// A Windows username for the target server
		/// </summary>
		public string Username { get { return _username; } }
		/// <summary>
		/// The Windows password for <see cref="Username"/>
		/// </summary>
		public string Password { internal get; set; }
		/// <summary>
		/// Check if the <see cref="RemoteLoginInfo"/> has been initialized with a <see cref="Password"/>
		/// </summary>
		[JsonIgnore]
		public bool HasPassword { get { return !String.IsNullOrWhiteSpace(Password); }  }

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
		/// Construct a <see cref="RemoteLoginInfo"/>
		/// </summary>
		/// <param name="ip">The value for <see cref="IP"/></param>
		/// <param name="port">The value for <see cref="Port"/></param>
		/// <param name="username">The value for <see cref="Username"/></param>
		/// <param name="password">The value for <see cref="Password"/></param>
		public RemoteLoginInfo(string ip, ushort port, string username, string password)
		{
			if (String.IsNullOrWhiteSpace(ip))
				throw new InvalidOperationException("ip must be set!");
			_ip = ip;
			if (port == 0)
				throw new InvalidOperationException("port may not be 0!");
			_port = port;
			if (String.IsNullOrWhiteSpace(username))
				throw new InvalidOperationException("username must be set!");
			_username = username;
			Password = password;
		}

		/// <summary>
		/// Construct a <see cref="RemoteLoginInfo"/> from JSON
		/// </summary>
		/// <param name="json">The result of a call to <see cref="ToJSON"/></param>
		public RemoteLoginInfo(string json)
		{
			var dic = JsonConvert.DeserializeObject<IDictionary<string, object>>(json);
			var ip = (string)dic[nameof(IP)];
			if (String.IsNullOrWhiteSpace(ip))
				throw new InvalidOperationException("ip must be set!");
			_ip = ip;
			var port = (ushort)(int)dic[nameof(Port)];
			if (port == 0)
				throw new InvalidOperationException("port may not be 0!");
			_port = port;
			var username = (string)dic[nameof(Username)];
			if (String.IsNullOrWhiteSpace(username))
				throw new InvalidOperationException("username must be set!");
			_username = username;
			if(dic.ContainsKey(nameof(Password)))
				Password = Helpers.DecryptData((string)dic[nameof(Password)], (string)dic[String.Format(EntropyFormatter, nameof(Password))]);
		}

		/// <summary>
		/// Returns <see cref="IP"/>
		/// </summary>
		/// <returns><see cref="IP"/></returns>
		public override string ToString()
		{
			return IP;
		}

		/// <summary>
		/// Checks if another <see cref="RemoteLoginInfo"/> matches <see langword="this"/> one
		/// </summary>
		/// <param name="other">Another <see cref="RemoteLoginInfo"/></param>
		/// <returns><see langword="true"/> if <see langword="this"/> and <paramref name="other"/> have the same <see cref="IP"/>, <see cref="Port"/>, and <see cref="Username"/></returns>
		public bool Equals(RemoteLoginInfo other)
		{
			return other != null && IP == other.IP && Port == other.Port && Username == other.Username;
		}

		/// <summary>
		/// Returns a JSON representation of the <see cref="RemoteLoginInfo"/> with the <see cref="Password"/> encrypted
		/// </summary>
		/// <returns>A JSON representation of the <see cref="RemoteLoginInfo"/></returns>
		public string ToJSON()
		{
			//serialize it to a dic first so we can store the entropy
			var raw = JsonConvert.SerializeObject(this);
			var dic = JsonConvert.DeserializeObject<IDictionary<string, object>>(raw);
			if (HasPassword)
			{
				dic.Add(nameof(Password), Helpers.EncryptData(Password, out string entropy));
				dic.Add(String.Format(EntropyFormatter, nameof(Password)), entropy);
			}
			return JsonConvert.SerializeObject(dic);
		}
	}
}
