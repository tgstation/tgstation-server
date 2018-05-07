using System;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class InstanceFactory : IInstanceFactory
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceFactory"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Construct an <see cref="InstanceFactory"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		public InstanceFactory(IIOManager ioManager) => this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));

		/// <inheritdoc />
		public IInstance CreateInstance(Host.Models.Instance metadata, IDatabaseContextFactory databaseContextFactory)
		{
			//Create the ioManager for the instance

			var instanceIoManager = new ResolvingIOManager(ioManager, metadata.Path);

			//various other ioManagers
			var repoIoManager = new ResolvingIOManager(instanceIoManager, "Repository");
			var byondIOManager = new ResolvingIOManager(instanceIoManager, "Byond");
			var gameIoManager = new ResolvingIOManager(instanceIoManager, "Game");
			var configurationIoManager = new ResolvingIOManager(instanceIoManager, "Configuration");
			var codeModificationsIoMananger = new ResolvingIOManager(instanceIoManager, "CodeModifications");

			var dmbFactory = new DmbFactory(databaseContextFactory, gameIoManager);



			throw new NotImplementedException();
		}
	}
}
