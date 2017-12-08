using System;
using System.Collections.Generic;
using TGS.Interface.Components;

namespace TGS.Interface.Wrappers
{
	/// <inheritdoc />
	sealed class Server : IServer
	{
		/// <inheritdoc />
		public Version Version => serverInterface.ServerVersion;

		/// <inheritdoc />
		public IEnumerable<IInstance> Instances { get
			{
				lock (this)
					if(knownInstances == null)
						knownInstances = serverInterface.GetComponent<ITGLanding>(null).ListInstances();
				foreach(var I in knownInstances)
					yield return new Instance(serverInterface, I);
			}
		}

		/// <inheritdoc />
		public ITGInstanceManager InstanceManager => serverInterface.GetComponent<ITGInstanceManager>(null);

		/// <summary>
		/// The backing <see cref="ServerInterface"/>
		/// </summary>
		readonly ServerInterface serverInterface;

		/// <summary>
		/// Result of a call to <see cref="ITGLanding.ListInstances"/>
		/// </summary>
		IList<InstanceMetadata> knownInstances;

		/// <summary>
		/// Construct an <see cref="Server"/>
		/// </summary>
		/// <param name="_serverInterface">The <see cref="ServerInterface"/> to use</param>
		public Server(ServerInterface _serverInterface)
		{
			serverInterface = _serverInterface;
		}

		/// <inheritdoc />
		public IInstance GetInstance(string name)
		{
			return new Instance(serverInterface, new InstanceMetadata { Name = name, Enabled = false });
		}

		/// <inheritdoc />
		public void PrepareForUpdate()
		{
			serverInterface.GetComponent<ITGSService>(null).PrepareForUpdate();
		}

		/// <inheritdoc />
		public string PythonPath()
		{
			return serverInterface.GetComponent<ITGSService>(null).PythonPath();
		}

		/// <inheritdoc />
		public ushort RemoteAccessPort()
		{
			return serverInterface.GetComponent<ITGSService>(null).RemoteAccessPort();
		}

		/// <inheritdoc />
		public bool SetPythonPath(string path)
		{
			return serverInterface.GetComponent<ITGSService>(null).SetPythonPath(path);
		}

		/// <inheritdoc />
		public string SetRemoteAccessPort(ushort port)
		{
			return serverInterface.GetComponent<ITGSService>(null).SetRemoteAccessPort(port);
		}

		/// <inheritdoc />
		string ITGSService.Version()
		{
			return serverInterface.GetComponent<ITGSService>(null).Version();
		}
	}
}
