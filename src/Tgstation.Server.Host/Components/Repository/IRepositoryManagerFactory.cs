using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.IO;

#nullable disable

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Factory for creating <see cref="IRepositoryManager"/>s.
	/// </summary>
	interface IRepositoryManagerFactory : IComponentService
	{
		/// <summary>
		/// Create a <see cref="IRepositoryManager"/>.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> to use.</param>
		/// <returns>A new <see cref="IRepositoryManager"/>.</returns>
		IRepositoryManager CreateRepositoryManager(IIOManager ioManager, IEventConsumer eventConsumer);
	}
}
