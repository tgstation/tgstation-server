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
		readonly string instanceName;

		/// <summary>
		/// Construct an <see cref="Instance"/>
		/// </summary>
		/// <param name="_serverInterface">The <see cref="ServerInterface"/> to use</param>
		/// <param name="_instanceName">The <see cref="IInstance"/> name to use</param>
		public Instance(ServerInterface _serverInterface, string _instanceName)
		{
			serverInterface = _serverInterface;
			instanceName = _instanceName;
		}

		/// <inheritdoc />
		public ITGAdministration Administration => serverInterface.GetComponent<ITGAdministration>(instanceName);

		/// <inheritdoc />
		public ITGByond Byond => serverInterface.GetComponent<ITGByond>(instanceName);

		/// <inheritdoc />
		public ITGChat Chat => serverInterface.GetComponent<ITGChat>(instanceName);

		/// <inheritdoc />
		public ITGCompiler Compiler => serverInterface.GetComponent<ITGCompiler>(instanceName);

		/// <inheritdoc />
		public ITGConfig Config => serverInterface.GetComponent<ITGConfig>(instanceName);

		/// <inheritdoc />
		public ITGDreamDaemon DreamDaemon => serverInterface.GetComponent<ITGDreamDaemon>(instanceName);

		/// <inheritdoc />
		public ITGInterop Interop => serverInterface.GetComponent<ITGInterop>(instanceName);

		/// <inheritdoc />
		public ITGRepository Repository => serverInterface.GetComponent<ITGRepository>(instanceName);

		/// <inheritdoc />
		public string ServerDirectory()
		{
			return serverInterface.GetComponent<ITGInstance>(instanceName).ServerDirectory();
		}

		/// <inheritdoc />
		public string Version()
		{
			return serverInterface.GetComponent<ITGInstance>(instanceName).Version();
		}
	}
}
