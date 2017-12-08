using System;
using System.Linq;
using TGS.Interface.Components;

namespace TGS.Interface
{
	/// <inheritdoc />
	sealed class Instance : IInstance
	{
		/// <inheritdoc />
		public InstanceMetadata Metadata
		{
			get
			{
				if (!metadata.Enabled)
					//metadata needs populating
					metadata = serverInterface.GetComponent<ITGLanding>(null).ListInstances().Where(x => x.Name == metadata.Name).First();
				return metadata;
			}
		}

		/// <summary>
		/// Whether or not the current user is known to be an administrator of the <see cref="IInstance"/>
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
						serverInterface.GetComponent<ITGAdministration>(metadata.Name).GetCurrentAuthorizedGroup();
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
		/// The name of the <see cref="IInstance"/>
		/// </summary>
		InstanceMetadata metadata;
		/// <summary>
		/// Backing field for <see cref="UserIsAdministrator"/>
		/// </summary>
		bool isAdministrator;

		/// <summary>
		/// Construct an <see cref="Instance"/>
		/// </summary>
		/// <param name="_serverInterface">The <see cref="Client"/> to use</param>
		/// <param name="_metadata">The <see cref="InstanceMetadata"/> for the <see cref="IInstance"/></param>
		public Instance(Client _serverInterface, InstanceMetadata _metadata)
		{
			serverInterface = _serverInterface;
			metadata = _metadata;
			if (metadata.Enabled)
				//run a connectivity check
				serverInterface.GetComponent<ITGConnectivity>(metadata.Name).VerifyConnection();
		}

		/// <summary>
		/// Used for implementing the <see cref="ITGInstance"/> component of <see cref="IInstance"/>
		/// </summary>
		ITGInstance InstanceComponent => serverInterface.GetComponent<ITGInstance>(metadata.Name);

		/// <summary>
		/// Get a string representation of the <see cref="Instance"/>
		/// </summary>
		/// <returns>A string representation of the <see cref="Instance"/></returns>
		public override string ToString()
		{
			return String.Format("{0} - {1} - {2}", metadata.Name, metadata.Path, metadata.Enabled ? "ONLINE" : "OFFLINE");
		}

		/// <inheritdoc />
		public ITGAdministration Administration => UserIsAdministrator ? serverInterface.GetComponent<ITGAdministration>(metadata.Name) : null;

		/// <inheritdoc />
		public ITGByond Byond => serverInterface.GetComponent<ITGByond>(metadata.Name);

		/// <inheritdoc />
		public ITGChat Chat => serverInterface.GetComponent<ITGChat>(metadata.Name);

		/// <inheritdoc />
		public ITGCompiler Compiler => serverInterface.GetComponent<ITGCompiler>(metadata.Name);

		/// <inheritdoc />
		public ITGStatic StaticFiles => serverInterface.GetComponent<ITGStatic>(metadata.Name);

		/// <inheritdoc />
		public ITGDreamDaemon DreamDaemon => serverInterface.GetComponent<ITGDreamDaemon>(metadata.Name);

		/// <inheritdoc />
		public ITGInterop Interop => serverInterface.GetComponent<ITGInterop>(metadata.Name);

		/// <inheritdoc />
		public ITGRepository Repository => serverInterface.GetComponent<ITGRepository>(metadata.Name);

		/// <inheritdoc />
		public string ServerDirectory()
		{
			return InstanceComponent.ServerDirectory();
		}

		/// <inheritdoc />
		public string Version()
		{
			return InstanceComponent.Version();
		}

		/// <inheritdoc />
		public string UpdateTGS3Json()
		{
			return InstanceComponent.UpdateTGS3Json();
		}

		/// <inheritdoc />
		public void SetAutoUpdateInterval(ulong newInterval)
		{
			InstanceComponent.SetAutoUpdateInterval(newInterval);
		}

		/// <inheritdoc />
		public ulong AutoUpdateInterval()
		{
			return InstanceComponent.AutoUpdateInterval();
		}
	}
}
