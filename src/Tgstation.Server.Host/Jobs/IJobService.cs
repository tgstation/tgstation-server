using Tgstation.Server.Host.Components;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// The service that manages everything to do with jobs.
	/// </summary>
	public interface IJobService : IJobManager, IComponentService
	{
		/// <summary>
		/// Activate the <see cref="IJobManager"/>.
		/// </summary>
		/// <param name="instanceCoreProvider">The <see cref="IInstanceCoreProvider"/> for the <see cref="IJobManager"/>.</param>
		void Activate(IInstanceCoreProvider instanceCoreProvider);
	}
}
