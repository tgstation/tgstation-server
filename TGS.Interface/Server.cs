using System;
using System.Collections.Generic;
using TGS.Interface.Components;

namespace TGS.Interface
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
					if (knownInstances == null)
						knownInstances = serverInterface.GetComponent<ITGLanding>(null).ListInstances();
				foreach (var I in knownInstances)
				{
					IInstance nextInstance;
					try
					{
						nextInstance = new Instance(serverInterface, I);
					}
					catch
					{
						continue;
					}
					yield return nextInstance;
				}
			}
		}

		/// <inheritdoc />
		public ITGInstanceManager InstanceManager => UserIsAdministrator ? serverInterface.GetComponent<ITGInstanceManager>(null) : null;

		/// <inheritdoc />
		public ITGSService Management => UserIsAdministrator ? serverInterface.GetComponent<ITGSService>(null) : null;

		/// <summary>
		/// If the connected user is an administrator of the <see cref="IServer"/>
		/// </summary>
		bool UserIsAdministrator
		{
			get
			{
				lock (this)
				{
					if (isAdministrator)
						return true;
					try
					{
						serverInterface.GetComponent<ITGSService>(null).Version();
						isAdministrator = true;
						return true;
					}
					catch
					{
						return false;
					}
				}
			}
		}

		/// <summary>
		/// The backing <see cref="Client"/>
		/// </summary>
		readonly Client serverInterface;

		/// <summary>
		/// Result of a call to <see cref="ITGLanding.ListInstances"/>
		/// </summary>
		IList<InstanceMetadata> knownInstances;

		/// <summary>
		/// Backing field for <see cref="UserIsAdministrator"/>
		/// </summary>
		bool isAdministrator;

		/// <summary>
		/// Construct an <see cref="Server"/>
		/// </summary>
		/// <param name="_serverInterface">The <see cref="Client"/> to use</param>
		public Server(Client _serverInterface)
		{
			serverInterface = _serverInterface;
		}

		/// <inheritdoc />
		public IInstance GetInstance(string name)
		{
			return new Instance(serverInterface, new InstanceMetadata { Name = name, Enabled = false });
		}
	}
}
