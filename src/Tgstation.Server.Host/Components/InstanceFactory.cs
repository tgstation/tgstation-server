using System;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceFactory : IInstanceFactory
	{
		readonly IIOManager ioManager;

		public InstanceFactory(IIOManager ioManager) => this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		/// <inheritdoc />
		public IInstance CreateInstance(Host.Models.Instance metadata)
		{
			//Create the ioManager for the instance

			var instanceIoManager = new ResolvingIOManager(ioManager, metadata.Path);

			//various other ioManagers
			var repoIoManager = new ResolvingIOManager(instanceIoManager, "Repository");
			var byondIOManager = new ResolvingIOManager(instanceIoManager, "Byond");
			var gameIoManager = new ResolvingIOManager(instanceIoManager, "Game");
			var configurationIoManager = new ResolvingIOManager(instanceIoManager, "Configuration");

			throw new NotImplementedException();
		}
	}
}
