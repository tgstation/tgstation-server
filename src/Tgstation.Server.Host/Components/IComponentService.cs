using Microsoft.Extensions.Hosting;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents a component meant to be started and stopped by its parent component.
	/// </summary>
	public interface IComponentService : IHostedService
	{
	}
}
