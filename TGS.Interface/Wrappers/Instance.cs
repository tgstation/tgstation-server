using System.Linq;
using TGS.Interface.Components;

namespace TGS.Interface.Wrappers
{
	/// <inheritdoc />
	sealed class Instance : IInstance
	{
		/// <summary>
		/// The backing <see cref="ServerInterface"/>
		/// </summary>
		readonly ServerInterface serverInterface;
		/// <summary>
		/// The name of the <see cref="IInstance"/>
		/// </summary>
		InstanceMetadata metadata;

		/// <summary>
		/// Construct an <see cref="Instance"/>
		/// </summary>
		/// <param name="_serverInterface">The <see cref="ServerInterface"/> to use</param>
		/// <param name="_metadata">The <see cref="InstanceMetadata"/> for the <see cref="IInstance"/></param>
		public Instance(ServerInterface _serverInterface, InstanceMetadata _metadata)
		{
			serverInterface = _serverInterface;
			metadata = _metadata;
			if (metadata.Enabled)
				//run a connectivity check
				serverInterface.GetComponent<ITGConnectivity>(metadata.Name).VerifyConnection();
		}

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

		/// <inheritdoc />
		public ITGAdministration Administration => serverInterface.GetComponent<ITGAdministration>(metadata.Name);

		/// <inheritdoc />
		public ITGByond Byond => serverInterface.GetComponent<ITGByond>(metadata.Name);

		/// <inheritdoc />
		public ITGChat Chat => serverInterface.GetComponent<ITGChat>(metadata.Name);

		/// <inheritdoc />
		public ITGCompiler Compiler => serverInterface.GetComponent<ITGCompiler>(metadata.Name);

		/// <inheritdoc />
		public ITGConfig Config => serverInterface.GetComponent<ITGConfig>(metadata.Name);

		/// <inheritdoc />
		public ITGDreamDaemon DreamDaemon => serverInterface.GetComponent<ITGDreamDaemon>(metadata.Name);

		/// <inheritdoc />
		public ITGInterop Interop => serverInterface.GetComponent<ITGInterop>(metadata.Name);

		/// <inheritdoc />
		public ITGRepository Repository => serverInterface.GetComponent<ITGRepository>(metadata.Name);

		/// <inheritdoc />
		public string ServerDirectory()
		{
			return serverInterface.GetComponent<ITGInstance>(metadata.Name).ServerDirectory();
		}

		/// <inheritdoc />
		public string Version()
		{
			return serverInterface.GetComponent<ITGInstance>(metadata.Name).Version();
		}
	}
}
