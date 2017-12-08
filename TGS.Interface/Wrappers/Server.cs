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
		public ITGInstanceManager InstanceManager => serverInterface.GetComponent<ITGInstanceManager>(null);

		/// <inheritdoc />
		public ITGSService Management
		{
			get
			{
				var component = serverInterface.GetComponent<ITGSService>(null);
				lock (this)
				{
					if (!userIsAdministrator)
						try
						{
							var test = component.Version();
							userIsAdministrator = true;
						}
						catch
						{
							return null;
						}
				}
				return component;
			}
		}

		/// <summary>
		/// The backing <see cref="ServerInterface"/>
		/// </summary>
		readonly ServerInterface serverInterface;
		/// <summary>
		/// If the connected user is an administrator of the <see cref="IServer"/>
		/// </summary>
		bool userIsAdministrator;

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
	}
}
